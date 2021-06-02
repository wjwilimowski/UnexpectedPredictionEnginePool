# Unexpected behavior in ML.NET PredictionEnginePool

### Preface

We're using a named prediction engine with the name `model1` in a `PredictionEnginePool`.

```c#
services
    .AddPredictionEnginePool<ModelInput, ModelOutput>()
    .FromFile(modelName: "model1", filePath: "MLModel.zip", watchForChanges: false);
```

Example usage:

```c#
private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;

public void ThisWorks()
{
    // This will initialize the "model1" prediction engine if it hasn't been used yet
    // See source code:
    // https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L81
    PredictionEngine<ModelInput, ModelOutput> engine = _pool.GetPredictionEngine("model1");
    
    // We can now use the engine, then return it to the pool
}
```

In the above snippet, the `PredictionEnginePool.GetPredictionEngine(string modelName)` method ensures the engine is loaded before use.

https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L101-L105

### The issue

If `_pool.GetPredictionEngine("model1")` has never been called before calling `_pool.GetModel("model1")`, the `model1` prediction engine is not loaded and `KeyNotFoundException` will be thrown.

```c#
private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;

public void ThisDoesNotWork()
{
    // Throws KeyNotFoundException unless _pool.GetPredictionEngine("model1") has been called before
    // See source code:
    // https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L51
    ITransformer model = _pool.GetModel("model1");
}
```

This inconsistent behavior can easily catch someone off-guard - one expects every public method on `PredictionEnginePool` to have the same "ensure initialized" behavior.

https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L51-L54
