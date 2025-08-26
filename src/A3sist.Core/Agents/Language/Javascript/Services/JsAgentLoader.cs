﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jint;

namespace A3sist.Orchastrator.Agents.JavaScript.Services
{
    public class JsAgentLoader
    {
        private readonly string _scriptsPath;
        private readonly List<JsAnalyzerWrapper> _analyzers = new();

        public IReadOnlyList<JsAnalyzerWrapper> Analyzers => _analyzers;

        public JsAgentLoader(string? scriptsPath = null)
        {
            _scriptsPath = scriptsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_scriptsPath))
                throw new DirectoryNotFoundException($"Scripts folder not found: {_scriptsPath}");

            var jsFiles = Directory.GetFiles(_scriptsPath, "*.js");

            foreach (var file in jsFiles)
            {
                var jsCode =  File.ReadAllText(file);
                var analyzer = new JsAnalyzerWrapper(jsCode);
                await analyzer.InitializeAsync();
                _analyzers.Add(analyzer);
            }
        }

        public async Task ShutdownAsync()
        {
            foreach (var analyzer in _analyzers)
            {
                await analyzer.ShutdownAsync();
            }
            _analyzers.Clear();
        }

        public void Dispose()
        {
            foreach (var analyzer in _analyzers)
            {
                analyzer?.Dispose();
            }
            _analyzers.Clear();
        }
    }
}
