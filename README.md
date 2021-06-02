# Unexpected behavior in ML.NET PredictionEnginePool

## Preface

We're using a named prediction engine with the name `model1` in a `PredictionEnginePool`.

```c#
services
    .AddPredictionEnginePool<ModelInput, ModelOutput>()
    .FromFile(modelName: "model1", filePath: "MLModel.zip", watchForChanges: false);
```

The below usage works as expected because the `PredictionEnginePool` ensures the engine is lazy loaded before use.

```c#
private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;

public SomeService(PredictionEnginePool<ModelInput, ModelOutput> pool)
{
    _pool = pool;
}

public ModelOutput Predict(ModelInput input)
{
    // This will initialize the "model1" prediction engine if it hasn't been used yet
    //
    // See source code:
    // https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePoolExtensions.cs#L39
    // https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L81
    ModelOutput result = _pool.Predict("model1", input);
    
    return result;
}
```

## The problem

For whatever reason, one might want to get access to the raw `ITransformer` powering the `PredictionEngine`, which seems reasonable given that `PredictionEnginePool` has a public method for that. However, there is a catch.

```c#
private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;
private readonly MLContext _context;

public SomeService(PredictionEnginePool<ModelInput, ModelOutput> pool, MLContext context)
{
    _pool = pool;
    _context = context;
}

public ITransformer GetModelTransformer()
{
    // This method does NOT initialize the named engine if it hasn't been initialized yet.
    // It will throw KeyNotFoundException if called before something has initialized "model1",
    // but work just fine after something else has already initialized it.
    //
    // See source code:
    // https://github.com/dotnet/machinelearning/blob/58450d4f0709c237de95f31f8f05d46983c7a5c0/src/Microsoft.Extensions.ML/PredictionEnginePool.cs#L53
    ITransformer model = _pool.GetModel("model1");
    
    return model;
}
```

This issue can prove confusing, because a consumer of the library would reasonably expect a public method of `PredictionEnginePool` to take care of initializing everything under the hood. 

Getting the named engine from the pool and returning it immediately on app startup can be used as a workaround.

```c#
// Call once
public void InitializeModel()
{
    var predictionEngine = _pool.GetPredictionEngine("model1");
    _pool.ReturnPredictionEngine("model1", predictionEngine);
}

// If InitializeModel() was called first, this works
public ITransformer GetModelTransformer()
{
    ITransformer model = _pool.GetModel("model1");
    
    return model;
}
```