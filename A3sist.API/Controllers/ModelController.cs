using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;
using A3sist.API.Services;
using A3sist.API.Hubs;

namespace A3sist.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    private readonly IModelManagementService _modelService;
    private readonly IHubContext<A3sistHub> _hubContext;
    private readonly ILogger<ModelController> _logger;

    public ModelController(
        IModelManagementService modelService,
        IHubContext<A3sistHub> hubContext,
        ILogger<ModelController> logger)
    {
        _modelService = modelService;
        _hubContext = hubContext;
        _logger = logger;
        
        // Subscribe to model change events
        _modelService.ActiveModelChanged += OnActiveModelChanged;
    }

    /// <summary>
    /// Get all available models
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModelInfo>>> GetModels()
    {
        try
        {
            var models = await _modelService.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models");
            return StatusCode(500, new { error = "Failed to get models" });
        }
    }

    /// <summary>
    /// Get the currently active model
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ModelInfo>> GetActiveModel()
    {
        try
        {
            var activeModel = await _modelService.GetActiveModelAsync();
            if (activeModel == null)
                return NotFound(new { error = "No active model set" });

            return Ok(activeModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active model");
            return StatusCode(500, new { error = "Failed to get active model" });
        }
    }

    /// <summary>
    /// Set the active model
    /// </summary>
    [HttpPut("active/{modelId}")]
    public async Task<ActionResult<bool>> SetActiveModel(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "Model ID is required" });

            var success = await _modelService.SetActiveModelAsync(modelId);
            if (!success)
                return BadRequest(new { error = "Failed to set active model. Model may not exist or be unavailable." });

            return Ok(new { success = true, message = "Active model set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active model {ModelId}", modelId);
            return StatusCode(500, new { error = "Failed to set active model" });
        }
    }

    /// <summary>
    /// Add a new model
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<bool>> AddModel([FromBody] ModelInfo model)
    {
        try
        {
            if (model == null)
                return BadRequest(new { error = "Model information is required" });

            if (string.IsNullOrEmpty(model.Id) || string.IsNullOrEmpty(model.Name))
                return BadRequest(new { error = "Model ID and Name are required" });

            var success = await _modelService.AddModelAsync(model);
            if (!success)
                return BadRequest(new { error = "Failed to add model" });

            _logger.LogInformation("Model {ModelName} added via API", model.Name);
            return Ok(new { success = true, message = "Model added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding model {ModelName}", model?.Name);
            return StatusCode(500, new { error = "Failed to add model" });
        }
    }

    /// <summary>
    /// Update an existing model
    /// </summary>
    [HttpPut("{modelId}")]
    public async Task<ActionResult<bool>> UpdateModel(string modelId, [FromBody] ModelInfo model)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "Model ID is required" });

            if (model == null)
                return BadRequest(new { error = "Model information is required" });

            // Ensure the model ID matches
            model.Id = modelId;

            var success = await _modelService.AddModelAsync(model); // AddModel handles updates too
            if (!success)
                return BadRequest(new { error = "Failed to update model" });

            _logger.LogInformation("Model {ModelName} updated via API", model.Name);
            return Ok(new { success = true, message = "Model updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model {ModelId}", modelId);
            return StatusCode(500, new { error = "Failed to update model" });
        }
    }

    /// <summary>
    /// Remove a model
    /// </summary>
    [HttpDelete("{modelId}")]
    public async Task<ActionResult<bool>> RemoveModel(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "Model ID is required" });

            var success = await _modelService.RemoveModelAsync(modelId);
            if (!success)
                return NotFound(new { error = "Model not found" });

            _logger.LogInformation("Model {ModelId} removed via API", modelId);
            return Ok(new { success = true, message = "Model removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing model {ModelId}", modelId);
            return StatusCode(500, new { error = "Failed to remove model" });
        }
    }

    /// <summary>
    /// Test connection to a specific model
    /// </summary>
    [HttpPost("{modelId}/test")]
    public async Task<ActionResult<bool>> TestModel(string modelId)
    {
        try
        {
            if (string.IsNullOrEmpty(modelId))
                return BadRequest(new { error = "Model ID is required" });

            var success = await _modelService.TestModelConnectionAsync(modelId);
            return Ok(new { 
                success = success, 
                message = success ? "Model connection successful" : "Model connection failed" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing model {ModelId}", modelId);
            return StatusCode(500, new { error = "Failed to test model connection" });
        }
    }

    /// <summary>
    /// Send a request to the active model
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<ModelResponse>> SendRequest([FromBody] ModelRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request is required" });

            if (string.IsNullOrEmpty(request.Prompt))
                return BadRequest(new { error = "Prompt is required" });

            var response = await _modelService.SendRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending model request");
            return StatusCode(500, new { error = "Failed to send model request" });
        }
    }

    private async void OnActiveModelChanged(object? sender, ModelChangedEventArgs e)
    {
        try
        {
            // Notify all connected clients about the active model change
            await _hubContext.Clients.All.SendAsync("ActiveModelChanged", e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting active model change");
        }
    }
}