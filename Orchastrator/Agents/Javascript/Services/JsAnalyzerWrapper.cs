using System.Threading.Tasks;
using EnvDTE80;
using Jint;

namespace A3sist.Orchastrator.Agents.JavaScript.Services
{
    public class JsAnalyzerWrapper
    {
        private readonly Jint.Engine _engine;

        public string Name { get; }

        public JsAnalyzerWrapper(string jsCode)
        {
            _engine = new Jint.Engine(cfg => cfg.AllowClr());
            _engine.Execute(jsCode);

            // Instantiate analyzer object from JS
            _engine.Execute("var analyzer = new Analyzer();");

            Name = _engine.Evaluate("analyzer.constructor.name").AsString();
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() => _engine.Invoke("analyzer.initialize"));
        }

        public async Task<string> AnalyzeCodeAsync(string code)
        {
            return await Task.Run(() => _engine.Invoke("analyzer.analyzeCode", code).AsString());
        }

        public async Task ShutdownAsync()
        {
            await Task.Run(() => _engine.Invoke("analyzer.shutdown"));
        }
    }
}
