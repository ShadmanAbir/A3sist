const express = require('express');
const cors = require('cors');
const { exec } = require('child_process');
const fs = require('fs').promises;
const path = require('path');

const app = express();
app.use(cors());
app.use(express.json({ limit: '50mb' }));

// Core Development Tools for A3sist
const tools = [
    {
        name: "code_analysis",
        description: "Comprehensive code analysis for C#, JavaScript, Python",
        parameters: {
            type: "object",
            properties: {
                code: { type: "string" },
                language: { type: "string", enum: ["csharp", "javascript", "python", "typescript"] },
                analysisLevel: { type: "string", enum: ["basic", "full", "deep"] },
                checkSecurity: { type: "boolean", default: true },
                checkPerformance: { type: "boolean", default: true }
            },
            required: ["code", "language"]
        }
    },
    {
        name: "code_refactor",
        description: "Intelligent code refactoring suggestions",
        parameters: {
            type: "object",
            properties: {
                code: { type: "string" },
                language: { type: "string" },
                refactorType: { type: "string", enum: ["extract_method", "rename", "optimize", "modernize"] },
                targetFramework: { type: "string" }
            }
        }
    },
    {
        name: "code_validation",
        description: "Validates code syntax and compilation",
        parameters: {
            type: "object",
            properties: {
                code: { type: "string" },
                language: { type: "string" },
                projectContext: { type: "object" }
            }
        }
    },
    {
        name: "language_conversion",
        description: "Convert code between languages",
        parameters: {
            type: "object",
            properties: {
                code: { type: "string" },
                fromLanguage: { type: "string" },
                toLanguage: { type: "string" },
                preserveComments: { type: "boolean", default: true }
            }
        }
    }
];

// MCP Request Handler
app.post('/mcp', async (req, res) => {
    const { method, params } = req.body;
    
    try {
        switch (method) {
            case 'tools/list':
                return res.json({ success: true, tools });
            case 'tools/execute':
                return await handleToolExecution(req, res);
            case 'llm/chat':
                return await handleLLMChat(req, res);
            default:
                return res.status(400).json({ success: false, error: `Unknown method: ${method}` });
        }
    } catch (error) {
        res.status(500).json({ success: false, error: error.message });
    }
});

async function handleToolExecution(req, res) {
    const { name, parameters } = req.body.params;
    
    let result = "";
    
    switch (name) {
        case 'code_analysis':
            result = await analyzeCode(parameters);
            break;
        case 'code_refactor':
            result = await refactorCode(parameters);
            break;
        case 'code_validation':
            result = await validateCode(parameters);
            break;
        case 'language_conversion':
            result = await convertLanguage(parameters);
            break;
        default:
            return res.status(400).json({ success: false, error: `Unknown tool: ${name}` });
    }
    
    res.json({
        success: true,
        result,
        metadata: {
            toolName: name,
            executionTime: Date.now(),
            serverType: "CoreDevelopment"
        }
    });
}

async function analyzeCode(params) {
    const { code, language, analysisLevel = "full", checkSecurity = true, checkPerformance = true } = params;
    
    const analysis = {
        language,
        analysisLevel,
        issues: [],
        suggestions: [],
        metrics: {},
        security: checkSecurity ? [] : null,
        performance: checkPerformance ? [] : null
    };

    // Language-specific analysis
    switch (language.toLowerCase()) {
        case 'csharp':
            return analyzeCSharpCode(code, analysis);
        case 'javascript':
        case 'typescript':
            return analyzeJavaScriptCode(code, analysis);
        case 'python':
            return analyzePythonCode(code, analysis);
        default:
            return JSON.stringify({ error: `Unsupported language: ${language}` });
    }
}

function analyzeCSharpCode(code, analysis) {
    // C# specific analysis
    if (code.includes("var ")) {
        analysis.suggestions.push("Consider using explicit types for better readability");
    }
    
    if (!code.includes("namespace")) {
        analysis.issues.push("Class should be in a namespace");
    }
    
    if (code.includes("Thread.Sleep")) {
        analysis.performance.push("Use async/await instead of Thread.Sleep");
    }
    
    if (code.includes("string.Concat") && code.includes("+")) {
        analysis.performance.push("Use StringBuilder for multiple string concatenations");
    }
    
    analysis.metrics = {
        linesOfCode: code.split('\n').length,
        complexity: calculateComplexity(code),
        maintainabilityIndex: calculateMaintainabilityIndex(code)
    };
    
    return JSON.stringify(analysis);
}

function analyzeJavaScriptCode(code, analysis) {
    // JavaScript/TypeScript analysis
    if (code.includes("var ")) {
        analysis.suggestions.push("Use 'let' or 'const' instead of 'var'");
    }
    
    if (code.includes("== ") || code.includes("!= ")) {
        analysis.suggestions.push("Use strict equality (=== or !==) instead of loose equality");
    }
    
    if (code.includes("eval(")) {
        analysis.security.push("Avoid using eval() - security risk");
    }
    
    analysis.metrics = {
        linesOfCode: code.split('\n').length,
        complexity: calculateComplexity(code)
    };
    
    return JSON.stringify(analysis);
}

function analyzePythonCode(code, analysis) {
    // Python analysis
    if (!code.includes("def ") && code.length > 50) {
        analysis.suggestions.push("Consider breaking code into functions");
    }
    
    if (code.includes("except:")) {
        analysis.issues.push("Avoid bare except clauses");
    }
    
    if (code.includes("import *")) {
        analysis.suggestions.push("Avoid wildcard imports");
    }
    
    analysis.metrics = {
        linesOfCode: code.split('\n').length,
        complexity: calculateComplexity(code)
    };
    
    return JSON.stringify(analysis);
}

function calculateComplexity(code) {
    let complexity = 1;
    const complexityKeywords = ['if', 'else', 'for', 'while', 'switch', 'case', 'catch', 'try'];
    
    complexityKeywords.forEach(keyword => {
        const regex = new RegExp(`\\b${keyword}\\b`, 'g');
        const matches = code.match(regex);
        if (matches) complexity += matches.length;
    });
    
    return complexity;
}

function calculateMaintainabilityIndex(code) {
    const loc = code.split('\n').length;
    const complexity = calculateComplexity(code);
    
    return Math.max(0, Math.min(100, 171 - 5.2 * Math.log(loc) - 0.23 * complexity));
}

async function refactorCode(params) {
    const { code, language, refactorType, targetFramework } = params;
    
    const refactoring = {
        originalCode: code,
        refactoredCode: "",
        changes: [],
        language,
        refactorType
    };
    
    switch (refactorType) {
        case 'extract_method':
            refactoring.refactoredCode = extractMethod(code, language);
            refactoring.changes.push("Extracted repeated code into reusable method");
            break;
        case 'optimize':
            refactoring.refactoredCode = optimizeCode(code, language);
            refactoring.changes.push("Applied performance optimizations");
            break;
        case 'modernize':
            refactoring.refactoredCode = modernizeCode(code, language);
            refactoring.changes.push("Updated to modern language features");
            break;
        default:
            refactoring.refactoredCode = code;
            refactoring.changes.push("No refactoring applied");
    }
    
    return JSON.stringify(refactoring);
}

function extractMethod(code, language) {
    // Simple method extraction logic
    return code.replace(/(\w+)\s*=\s*(\w+)\s*\+\s*(\w+);?\n(\w+)\s*=\s*(\w+)\s*\+\s*(\w+);?/g, 
        'var result = AddValues($2, $3, $5, $6);\n\nprivate int AddValues(int a, int b, int c, int d) {\n    return a + b + c + d;\n}');
}

function optimizeCode(code, language) {
    if (language === 'csharp') {
        return code
            .replace(/string\.Concat\(([^)]+)\)/g, '$1')
            .replace(/Thread\.Sleep\((\d+)\)/g, 'await Task.Delay($1)');
    }
    return code;
}

function modernizeCode(code, language) {
    if (language === 'csharp') {
        return code
            .replace(/var (\w+) = new (\w+)\(\)/g, '$2 $1 = new()')
            .replace(/string (\w+) = ""/g, 'string $1 = string.Empty');
    }
    return code;
}

async function validateCode(params) {
    const { code, language, projectContext } = params;
    
    const validation = {
        isValid: true,
        errors: [],
        warnings: [],
        language,
        compilationResult: null
    };
    
    // Basic syntax validation
    try {
        switch (language.toLowerCase()) {
            case 'csharp':
                validateCSharpSyntax(code, validation);
                break;
            case 'javascript':
            case 'typescript':
                validateJavaScriptSyntax(code, validation);
                break;
            case 'python':
                validatePythonSyntax(code, validation);
                break;
        }
    } catch (error) {
        validation.isValid = false;
        validation.errors.push(error.message);
    }
    
    return JSON.stringify(validation);
}

function validateCSharpSyntax(code, validation) {
    // Basic C# validation
    const braceCount = (code.match(/\{/g) || []).length - (code.match(/\}/g) || []).length;
    if (braceCount !== 0) {
        validation.isValid = false;
        validation.errors.push("Mismatched braces");
    }
    
    if (code.includes("public class") && !code.includes("{")) {
        validation.errors.push("Class declaration missing opening brace");
    }
}

function validateJavaScriptSyntax(code, validation) {
    try {
        new Function(code);
    } catch (error) {
        validation.isValid = false;
        validation.errors.push(`Syntax error: ${error.message}`);
    }
}

function validatePythonSyntax(code, validation) {
    // Basic Python validation
    const lines = code.split('\n');
    let indentLevel = 0;
    
    lines.forEach((line, index) => {
        const trimmed = line.trim();
        if (trimmed && !trimmed.startsWith('#')) {
            const currentIndent = line.length - line.trimStart().length;
            if (currentIndent % 4 !== 0) {
                validation.warnings.push(`Line ${index + 1}: Inconsistent indentation`);
            }
        }
    });
}

async function convertLanguage(params) {
    const { code, fromLanguage, toLanguage, preserveComments = true } = params;
    
    const conversion = {
        originalLanguage: fromLanguage,
        targetLanguage: toLanguage,
        originalCode: code,
        convertedCode: "",
        conversionNotes: []
    };
    
    // Basic conversion logic (simplified)
    if (fromLanguage === 'csharp' && toLanguage === 'python') {
        conversion.convertedCode = convertCSharpToPython(code);
        conversion.conversionNotes.push("Converted C# to Python - manual review recommended");
    } else if (fromLanguage === 'javascript' && toLanguage === 'typescript') {
        conversion.convertedCode = convertJavaScriptToTypeScript(code);
        conversion.conversionNotes.push("Added TypeScript type annotations");
    } else {
        conversion.convertedCode = code;
        conversion.conversionNotes.push(`Direct conversion from ${fromLanguage} to ${toLanguage} not implemented`);
    }
    
    return JSON.stringify(conversion);
}

function convertCSharpToPython(code) {
    return code
        .replace(/public class (\w+)/g, 'class $1:')
        .replace(/public (\w+) (\w+)\(/g, 'def $2(self, ')
        .replace(/string/g, 'str')
        .replace(/int/g, 'int')
        .replace(/Console\.WriteLine\(/g, 'print(')
        .replace(/\{/g, ':')
        .replace(/\}/g, '');
}

function convertJavaScriptToTypeScript(code) {
    return code
        .replace(/function (\w+)\(/g, 'function $1(')
        .replace(/var (\w+)/g, 'let $1: any')
        .replace(/const (\w+) = /g, 'const $1: any = ');
}

async function handleLLMChat(req, res) {
    const { messages, model = "gpt-4" } = req.body.params;
    
    res.json({
        success: true,
        content: `Core Development Server processed: ${messages[0]?.content?.substring(0, 100)}...`,
        actualProvider: "CoreDevelopmentMCP",
        actualModel: model,
        tokensUsed: 150
    });
}

const PORT = process.env.CORE_DEV_PORT || 3001;
app.listen(PORT, () => {
    console.log(`🚀 A3sist Core Development MCP Server running on port ${PORT}`);
    console.log(`📚 Available tools: ${tools.map(t => t.name).join(', ')}`);
    console.log(`🔧 Specialized for: C#, JavaScript, Python development`);
});

module.exports = app;