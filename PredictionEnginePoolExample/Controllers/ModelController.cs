using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using SampleClassification.Model;

namespace PredictionEnginePoolExample.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ModelController : ControllerBase
	{
		private readonly PredictionEnginePool<ModelInput, ModelOutput> _pool;
		private readonly MLContext _context;

		public ModelController(PredictionEnginePool<ModelInput, ModelOutput> pool, MLContext context)
		{
			_pool = pool;
			_context = context;
		}

		[HttpPost("PredictWithEngine")]
		public IActionResult PredictWithEngine([FromBody] List<ModelInput> inputs)
		{
			// Works fine
			List<ModelOutput> result = inputs.Select(i => _pool.Predict("model1", i)).ToList();

			return Ok(result);
		}

		[HttpPost("PredictWithRawTransformer")]
		public IActionResult PredictWithRawTransformer([FromBody] List<ModelInput> inputs)
		{
			// Throws KeyNotFoundException unless _pool.Predict("model1", ...) has been called first
			ITransformer model = _pool.GetModel("model1");
			IDataView inputView = _context.Data.LoadFromEnumerable(inputs);

			IDataView predictions = model.Transform(inputView);
			IEnumerable<ModelOutput> result = _context.Data.CreateEnumerable<ModelOutput>(predictions, false, true);

			return Ok(result);
		}
	}
}