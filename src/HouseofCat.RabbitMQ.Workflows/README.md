# HouseofCat.RabbitMQ.Workflows

Still in progress.

```csharp
// Visualization
// 1.) InputBuffer -> Deserialize
//
// 2.) Deserialize -> ErrorBuffer
// 2.) Deserialize -> Decrypt
// 2.) Deserialize -> Decrypt -> Decompress
// 2.) Deserialize -> Decompress
// 2.) Deserialize -> ReadyBuffer
//
// 3.) Decrypt -> ErrorBuffer
// 3.) Decrypt -> Decompress (if not null) -> ReadForProcessing
// 3.) Decrypt -> ReadyBuffer
//
// 4.) Decompress -> Error
// 4.) Decompress -> ReadyBuffer
//
// Supplied Steps
// 5.) ReadyBuffer -> Step[0]
// 5.) Step[0] -> Error
// 5.) For n : Step(n) link to Step(n-1)
// 5.) For n : Step(n) link to ErrorBuffer
// 5.) Step(n) => PostProcessingBuffer
//
// 6.) PostProcessing -> PostCompression -> PostEncryption -> Finalization
// 6.) PostProcessing -> PostEncryption -> Finalization
// 6.) PostProcessing -> PostCompression -> Finalization
// 6.) PostProcessing -> Finalization
//
// 7.) PostCompression -> ErrorBuffer
// 7.) PostEncryption -> ErrorBuffer
// 7.) ErrorBuffer -> ErrorAction
```