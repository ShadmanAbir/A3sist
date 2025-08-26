const express = require('express');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json({ limit: '50mb' }));

const tools = [
    {
        name: "documentation_search",
        description: "Search technical documentation and API references",
        parameters: {
            type: "object",
            properties: {
                query: { type: "string" },
                scope: { type: "string", enum: ["dotnet", "javascript", "python", "general"] },
                docType: { type: "string", enum: ["api", "tutorial", "reference", "best-practices"] }
            }
        }
    },
    {
        name: "best_practices",
        description: "Get coding best practices and patterns",
        parameters: {
            type: "object",
            properties: {
                language: { type: "string" },
                category: { type: "string", enum: ["performance", "security", "maintainability", "design-patterns"] },
                framework: { type: "string" }
            }
        }
    },
    {
        name: "code_examples",
        description: "Find relevant code examples and snippets",
        parameters: {
            type: "object",
            properties: {
                technology: { type: "string" },
                pattern: { type: "string" },
                complexity: { type: "string", enum: ["beginner", "intermediate", "advanced"] }
            }
        }
    },
    {
        name: "knowledge_update",
        description: "Update knowledge base with new information",
        parameters: {
            type: "object",
            properties: {
                category: { type: "string" },
                content: { type: "string" },
                tags: { type: "array", items: { type: "string" } }
            }
        }
    }
];

app.post('/mcp', async (req, res) => {
    const { method, params } = req.body;
    
    try {
        switch (method) {
            case 'tools/list':
                return res.json({ success: true, tools });
            case 'tools/execute':
                return await handleToolExecution(req, res);
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
        case 'documentation_search':
            result = await searchDocumentation(parameters);
            break;
        case 'best_practices':
            result = await getBestPractices(parameters);
            break;
        case 'code_examples':
            result = await getCodeExamples(parameters);
            break;
        case 'knowledge_update':
            result = await updateKnowledge(parameters);
            break;
        default:
            return res.status(400).json({ success: false, error: `Unknown tool: ${name}` });
    }
    
    res.json({
        success: true,
        result,
        metadata: { toolName: name, serverType: "Knowledge" }
    });
}

async function searchDocumentation(params) {
    const { query, scope = "general", docType = "reference" } = params;
    
    const results = {
        query,
        scope,
        docType,
        results: [
            {
                title: `${scope.toUpperCase()} Documentation: ${query}`,
                content: `Comprehensive documentation for ${query} in ${scope}...`,
                url: `https://docs.microsoft.com/${scope}/${query.toLowerCase()}`,
                relevance: 0.95
            }
        ]
    };
    
    return JSON.stringify(results);
}

async function getBestPractices(params) {
    const { language, category, framework } = params;
    
    const practices = {
        language,
        category,
        framework,
        practices: [
            `Use async/await for better performance in ${language}`,
            `Follow SOLID principles for maintainable code`,
            `Implement proper error handling patterns`
        ]
    };
    
    return JSON.stringify(practices);
}

async function getCodeExamples(params) {
    const examples = {
        examples: [`Example code for ${params.technology}`]
    };
    
    return JSON.stringify(examples);
}

async function updateKnowledge(params) {
    return JSON.stringify({ success: true, updated: params.category });
}

const PORT = process.env.KNOWLEDGE_PORT || 3003;
app.listen(PORT, () => {
    console.log(`🚀 A3sist Knowledge MCP Server running on port ${PORT}`);
});