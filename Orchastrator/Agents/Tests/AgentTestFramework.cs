using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeAssist.Agents.Tests
{
    public class AgentTestFramework
    {
        private readonly AgentLifecycleManager _lifecycleManager;
        private readonly List<TestCase> _testCases = new List<TestCase>();

        public AgentTestFramework(AgentLifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        }

        public void AddTestCase(string testName, string agentName, Func<BaseAgent, Task<bool>> testFunction)
        {
            _testCases.Add(new TestCase(testName, agentName, testFunction));
        }

        public async Task RunAllTestsAsync()
        {
            var results = new List<TestResult>();

            foreach (var testCase in _testCases)
            {
                var result = await RunTestAsync(testCase);
                results.Add(result);
                Console.WriteLine(result.ToString());
            }

            Console.WriteLine($"Test Summary: {results.Count(r => r.Passed)} passed, {results.Count(r => !r.Passed)} failed");
        }

        private async Task<TestResult> RunTestAsync(TestCase testCase)
        {
            try
            {
                if (!_lifecycleManager.GetRegisteredAgents().Contains(testCase.AgentName))
                {
                    return new TestResult(testCase.TestName, false, $"Agent {testCase.AgentName} not registered");
                }

                var agentStatus = _lifecycleManager.GetAgentStatus(testCase.AgentName);
                if (agentStatus != BaseAgent.AgentStatus.Ready)
                {
                    return new TestResult(testCase.TestName, false, $"Agent {testCase.AgentName} not ready (status: {agentStatus})");
                }

                var passed = await testCase.TestFunction(_lifecycleManager.GetRegisteredAgents()[testCase.AgentName]);
                return new TestResult(testCase.TestName, passed, passed ? "Test passed" : "Test failed");
            }
            catch (Exception ex)
            {
                return new TestResult(testCase.TestName, false, $"Test threw exception: {ex.Message}");
            }
        }

        private class TestCase
        {
            public string TestName { get; }
            public string AgentName { get; }
            public Func<BaseAgent, Task<bool>> TestFunction { get; }

            public TestCase(string testName, string agentName, Func<BaseAgent, Task<bool>> testFunction)
            {
                TestName = testName;
                AgentName = agentName;
                TestFunction = testFunction;
            }
        }

        private class TestResult
        {
            public string TestName { get; }
            public bool Passed { get; }
            public string Message { get; }

            public TestResult(string testName, bool passed, string message)
            {
                TestName = testName;
                Passed = passed;
                Message = message;
            }

            public override string ToString()
            {
                return $"{TestName}: {(Passed ? "PASSED" : "FAILED")} - {Message}";
            }
        }
    }
}