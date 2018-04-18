# Qluent

 - [What is this?]
 - [How do I use it?]
   - [Creating a Qluent Queue]
   - [Basic Operations]
   - [Sending Messages]
   - [Receiving Messages]
   - [Receiving Messages & Controlling Deletion]
 - [Advanced Features]
   - [Message Visibility]
   - [Handling Poison Messages]
   - [Customising Serialization]
   - [Asynchronous Model]
   - [Logging]
 - [Background]
   - [Why do I need this?]
   - [Why did you build this?]
   - [What is this not?]
   - [Todo List]

---

## What is this?

***Qluent*** is a ***Fluent Queue Client***

Qluent provides a very simple Fluent API and wrapper classes around the Microsoft 
Azure Storage SDK, allowing you to interact with storage queues using 
strongly typed objects like this.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .BuildAsync();
    
var person = await q.PopAsync();
```

---

## How do I use it?

### Creating a Qluent Queue

By default the builder will create a queue connected to development storage.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .BuildAsync();
```

Or you can explicitly provide a connection string to a specific storage account.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .ConnectedToAccount("UseDevelopmentStorage=true")
    .UsingStorageQueue("my-test-queue")
    .BuildAsync();
```

---

### Basic Operations

You can clear all messages from a queue.

```csharp
await q.PurgeAsync();
```

You can check the approximate message count on a queue.

```csharp
var count = await q.CountAsync()
```

---

### Sending Messages

Queues are created for a specific type. You can push an object of 
that type directly to the queue.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .BuildAsync(); 
    
var person = new Person("John");
await q.PushAsync(person);
``` 

You can also push an `IEnumerable<T>` of messages to the queue.

```csharp
List<Person> people = new List<Person>();
await q.PushAsync(people);
```

---

### Receiving Messages

You can directly Pop an object off the queue. This will dequeue the 
`CloudQueueMessage`, attempt to deserialize it and if deserialization succeeds
remove it from the queue and return it to you.

If deserialization fails, the default behavior is to throw an exception. 
This will result in the message's dequeue count increasing, and it
reappearing on the queue after it's visibility timeout expires.

See: [Handling Poison Messages] for more info.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .BuildAsync(); 
    
var person = await q.PopAsync();
``` 

If you don't want to remove the object from the queue, you can peek at it instead.

```csharp
var person = await q.PeekAsync();
``` 

You can also Peek or Pop multiple messages at a time by passing a message count.

```csharp
IEnumerable<Person> peekedPeople = await q.PeekAsync(5);

IEnumerable<Person> poppedPeople = await q.PopAsync(5);
```

---

### Receiving Messages & Controlling Deletion

The Azure SDK supports a two phase dequeue process. First, the message is 
received from the queue. Second, the message is deleted from the queue. 
This allows a consumer to attempt processing in between these two steps, 
and if processing fails, the client can abort the operation and the message 
will appear on the queue again after it's visibilty timeout expires.

The previous `PopAsync` methods perform both Get & Delete operations in one.

If more control is required, by the consumer, Qluent also provides:
 - `GetAsync`, which returns a `IMessage<T>` wrapper object including the underlying message Id & PopReceipt
 - `DeleteAsync`, which accepts a `IMessage<T>`

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .BuildAsync(); 

var wrappedPerson = await q.GetAsync();

try
{    
    //attempt to process wrappedPerson.Value;
    await q.DeleteAsync(wrappedPerson);
}
catch(Exception ex){ ... }
```

---

## Advanced Features

### Message Visibility

You can provide a number of settings to override the various message visbility 
and time to live settings.

You can set a delay time before the message appears to consumers on the queue.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .ThatDelaysMessageVisibilityAfterEnqueuingFor(TimeSpan.FromMinutes(1))
    .BuildAsync();
``` 

You can specify the duration that a message remains invisible for after it's 
been dequeued, useful in combination with handling poison messages.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .ThatKeepsMessagesInvisibleAfterDequeuingFor(TimeSpan.FromMinutes(1))
    .BuildAsync();
``` 

You can specify the duration that message will remain alive on the queue if 
no consumers dequeue them.

```csharp
var q = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .ThatSetsAMessageTTLOf(TimeSpan.FromDays(1))
    .BuildAsync();
```

---

### Handling Poison Messages

When a message is removed from a Storage Queue, Qluent will attempt 
to deserialize it into the `<T>` you specified.

It is possible that deserialization might fail for a number of reasons. e.g. 
An unexpected/corrupted message may have been added to the queue which you cannot parse.

You can control how many times the library will attempt to dequeue and deserialize 
for you before it considers the message poison. Once considered poisonly, you can optionally
choose to route it to another queue for analysis/later processing.

```csharp
var jobQueue = await Builder
    .CreateAQueueOf<Person>()
    .UsingStorageQueue("my-test-queue")
    .ThatConsidersMessagesPoisonAfter(3)
    .AndSendsPoisonMessagesTo("my-poison-queue")
    .BuildAsync();
```

You can specify what should happen when a poison message is detected. The default behavior 
is to throw an exception each time the message fails to deserialize. 
You can override that behavior by specifying that exceptions should be swallowed. 
In this case, the Pop/Peek method will return null (or will remove the null result 
from an `IEnumerable<T>`).


```csharp
var jobQueue = await Builder
    .CreateAQueueOf<Job>()
    .UsingStorageQueue("my-test-queue")
    .ThatConsidersMessagesPoisonAfter(3)
    .AndSendsPoisonMessagesTo("my-poison-queue")
    .AndHandlesExceptionsOnPoisonMessages(By.SwallowingExceptions)
    .BuildAsync();
```

---

### Asynchronous Model

The library is built against the .NET Standard 2.0 to target both .NET Framework 
& .NET Core. All Operations are asynchronous and support a method overload to pass
a cancellation token.

```csharp
var person = await q.PopAsync();

CancellationToken ct = new CancellationToken(false);
var person = await q.PopAsync(ct);
```

Within the library all asynchronous calls are postpended with a call to `.ConfigureAwait(false)`.

During queue creation the library will perform an async operation to create the queue 
if it doesn't exist. Therefore the builder (and previous examples) provide an awaitable
`BuildAsync()` method. However if you need to create your queues in a non async manner
e.g. in a DI Container/Bootstrapper you can use the non async `Build` method.

```csharp
var q = Builder
    .CreateAQueueOf<Person>()
    .ConnectedToAccount("UseDevelopmentStorage=true")
    .UsingStorageQueue("my-test-queue")
    .Build();
```

---

### Customising Serialization

By default Qluent will serialize your entities to Json Strings using 
[Json.NET](https://www.newtonsoft.com/json).
Serialization is performed using the default `JsonConvert` utility.

For scenarios, where your client does not control both ends of the queue, you may have 
to deal with messages that have been serialized differently. 

To support this, Qluent allows your to pass your own custom binary or string serializer.

To create a custom binary serializer, implement the interface `Qluent.Serialization.IBinaryMessageSerializer<T>`
This will serialize/deserialize your message to a `byte[]` and push/pop it to the queue as bytes.
```csharp
var q = Builder
    .CreateAQueueOf<Person>()
    .ConnectedToAccount("UseDevelopmentStorage=true")
    .UsingStorageQueue("my-test-queue")
    .WithACustomSerializer(new CustomBinarySerializer())
    .Build();
``` 

To create a custom binary serializer, implement the interface `Qluent.Serialization.IStringMessageSerializer<T>`
This will serialize/deserialize your message to a `string` and push/pop it to the queue as string content.

```csharp
var q = Builder
    .CreateAQueueOf<Person>()
    .ConnectedToAccount("UseDevelopmentStorage=true")
    .UsingStorageQueue("my-test-queue")
    .WithACustomSerializer(new CustomStringSerializer())
    .Build();
``` 

---

### Logging

//Todo


---

### 2 phase commit using TransactionScopes

//Todo

---

## Background

### Why do I need this?

There's a lot of ceremony involved when using the SDK for Azure Storage Queues. 
Create an account, create a client, create a queue reference, make sure it exists,
object serialization/deserialization etc...

```csharp
var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
var queueClient = storageAccount.CreateCloudQueueClient();
var queue = queueClient.GetQueueReference("myqueue");
queue.CreateIfNotExists();

var person = new Person("John");
var serializedPerson = JsonConvert.Serialize(person)
var message = new CloudQueueMessage(serializedPerson); 
queue.AddMessage(message);

var result = queue.GetMessage();
var deserializedPerson = JsonConvert.Deserialize<Person>(result.AsString);
queue.DeleteMessage(result);
```

I'm also not a fan of the architectural decision in the SDK to leave settings like
message visbility up to the developer to decide on at the call site. If you're 
going to create your queues and access them via a DI framework, I'd prefer to 
centralize/standardize these settings at queue creation.

---

### Why did you build this?

This project was borne out frustration with a several different things. 

Firstly, my team and I have recently been working with Azure Durable Functions. They are still quite immature 
and given their static nature and the lack of support for DI frameworks like Autofac
I found we were having to either write a lot of boiler plate code or rely on the built
in binding/trigger capabilities which were lacking in certain amounts of control/capability. 
After seeing yet-another-queue-wrapper being written, I wanted to consolidate our approach.

Secondly, we have been wrestling with a legacy bug for a while now, which was the result of a predecessors
decision to take a dependency on a nuget package for which support had long since waned and for which
the source code was no longer available. After eventually decompiling and picking through some enterprise-fizz-buzz
queue code, the problem was discovered. 

Thirdly, I needed to scratch an itch :smirk:. I wanted to test out a number of things including building a fluent builder pattern, 
building a net standard 2.0 library that could be consumed by both NetFramework and NetCore20.

---

### What is this not?

This is not an Enterprise Service Bus. It is a simple wrapper around Azure Storage 
Queues to make working with them a little easier.

There are lots of complicated things you may find yourself doing in a 
distributed environment. Complex Retry Policies; complicated routing paths; 
Pub/Sub models involving topics and queues; the list goes on.

If you find yourself needing to do something complex like this, then perhaps you should 
be looking at a different technology stack (Azure Service Bus, Event Hubs, Event Grid, 
Kafka, NService Bus, Mulesoft etc...)

---

### Todo List

- ~~Interface based Refactoring~~
- ~~Document calls properly~~
- ~~Support Cancellation Tokens so that they can be passed through.~~
- ~~Support Pop Receipts so that the consumer can decide how to handle messages~~
- ~~Write up the docs around message visibility when the above is done~~
- ~~.NET Core Tests~~
- ~~Fix the TransactionScope stuff~~
  - Document it.
- Include NLog/ILogger calls so that you can hook in your logging framework
  - Docment it.