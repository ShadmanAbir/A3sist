using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;
using A3sist.Agents.Designer.Services;
using A3sist.Agents.Designer.Models;

namespace A3sist.Agents.Designer
{
    public class Designer : IAgent
    {
        private readonly ArchitectureAnalyzer _architectureAnalyzer;
        private readonly ScaffoldingGenerator _scaffoldingGenerator;
        private readonly DesignPlanner _designPlanner;
        private readonly PatternRecommender _patternRecommender;

        public string Name => "Designer";
        public AgentType Type => AgentType.Designer;
        public TaskStatus Status { get; private set; }

        public Designer()
        {
            _architectureAnalyzer = new ArchitectureAnalyzer();
            _scaffoldingGenerator = new ScaffoldingGenerator();
            _designPlanner = new DesignPlanner();
            _patternRecommender = new PatternRecommender();
            Status = TaskStatus.Pending;
        }

        public async Task InitializeAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _architectureAnalyzer.InitializeAsync(),
                _scaffoldingGenerator.InitializeAsync(),
                _designPlanner.InitializeAsync(),
                _patternRecommender.InitializeAsync()
            );
            Status = TaskStatus.Completed;
        }

        public async Task<AgentResponse> ExecuteAsync(AgentRequest request)
        {
            var response = new AgentResponse
            {
                RequestId = request.RequestId,
                AgentName = Name,
                TaskName = request.TaskName
            };

            try
            {
                Status = TaskStatus.InProgress;

                switch (request.TaskName.ToLower())
                {
                    case "architectureanalysis":
                        var analysis = await _architectureAnalyzer.AnalyzeArchitectureAsync(request.Context);
                        response.Result = JsonSerializer.Serialize(analysis);
                        break;

                    case "scaffolding":
                        var scaffolding = await _scaffoldingGenerator.GenerateScaffoldingAsync(request.Context);
                        response.Result = JsonSerializer.Serialize(scaffolding);
                        break;

                    case "designplanning":
                        var designPlan = await _designPlanner.CreateDesignPlanAsync(request.Context);
                        response.Result = JsonSerializer.Serialize(designPlan);
                        break;

                    case "patternrecommendation":
                        var patterns = await _patternRecommender.RecommendPatternsAsync(request.Context);
                        response.Result = JsonSerializer.Serialize(patterns);
                        break;

                    default:
                        throw new NotSupportedException($"Task {request.TaskName} is not supported by this agent");
                }

                response.IsSuccess = true;
                Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                Status = TaskStatus.Failed;
            }

            return response;
        }

        public async Task ShutdownAsync()
        {
            Status = TaskStatus.InProgress;
            await Task.WhenAll(
                _architectureAnalyzer.ShutdownAsync(),
                _scaffoldingGenerator.ShutdownAsync(),
                _designPlanner.ShutdownAsync(),
                _patternRecommender.ShutdownAsync()
            );
            Status = TaskStatus.Completed;
        }

        public async Task<AgentResponse> HandleMessageAsync(TaskMessage message)
        {
            // Handle incoming messages (e.g., from other agents)
            return await Task.FromResult(new AgentResponse
            {
                RequestId = message.MessageId,
                AgentName = Name,
                TaskName = "MessageHandling",
                Result = $"Message from {message.Sender} received",
                IsSuccess = true
            });
        }
    }
}