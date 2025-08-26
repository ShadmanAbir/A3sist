const express = require('express');
const cors = require('cors');
const fs = require('fs').promises;
const path = require('path');
const { exec } = require('child_process');

const app = express();
app.use(cors());
app.use(express.json({ limit: '50mb' }));

// Visual Studio Integration Tools
const tools = [
    {
        name: "project_analysis",
        description: "Analyze Visual Studio project structure and dependencies",
        parameters: {
            type: "object",
            properties: {
                projectPath: { type: "string" },
                solutionPath: { type: "string" },
                analysisType: { type: "string", enum: ["structure", "dependencies", "references", "full"] }
            }
        }
    },
    {
        name: "solution_management",
        description: "Manage Visual Studio solution operations",
        parameters: {
            type: "object",
            properties: {
                solutionPath: { type: "string" },
                operation: { type: "string", enum: ["build", "clean", "restore", "analyze"] },
                configuration: { type: "string", default: "Debug" }
            }
        }
    },
    {
        name: "nuget_operations",
        description: "Handle NuGet package operations",
        parameters: {
            type: "object",
            properties: {
                projectPath: { type: "string" },
                operation: { type: "string", enum: ["list", "install", "update", "remove", "search"] },
                packageName: { type: "string" },
                version: { type: "string" }
            }
        }
    },
    {
        name: "msbuild_operations",
        description: "Execute MSBuild operations and project compilation",
        parameters: {
            type: "object",
            properties: {
                projectPath: { type: "string" },
                target: { type: "string", default: "Build" },
                configuration: { type: "string", default: "Debug" },
                verbosity: { type: "string", default: "minimal" }
            }
        }
    },
    {
        name: "extension_integration",
        description: "Integrate with A3sist Visual Studio extension",
        parameters: {
            type: "object",
            properties: {
                operation: { type: "string", enum: ["status", "commands", "settings", "diagnostics"] },
                context: { type: "object" }
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
        case 'project_analysis':
            result = await analyzeProject(parameters);
            break;
        case 'solution_management':
            result = await manageSolution(parameters);
            break;
        case 'nuget_operations':
            result = await handleNuGet(parameters);
            break;
        case 'msbuild_operations':
            result = await handleMSBuild(parameters);
            break;
        case 'extension_integration':
            result = await handleExtensionIntegration(parameters);
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
            serverType: "VisualStudioIntegration"
        }
    });
}

async function analyzeProject(params) {
    const { projectPath, solutionPath, analysisType = "full" } = params;
    
    const analysis = {
        projectPath,
        solutionPath,
        analysisType,
        projectInfo: {},
        dependencies: [],
        structure: {},
        issues: [],
        recommendations: []
    };

    try {
        if (projectPath) {
            analysis.projectInfo = await analyzeProjectFile(projectPath);
            analysis.structure = await analyzeProjectStructure(projectPath);
        }
        
        if (solutionPath) {
            analysis.solutionInfo = await analyzeSolutionFile(solutionPath);
        }
        
        if (analysisType === "dependencies" || analysisType === "full") {
            analysis.dependencies = await analyzeDependencies(projectPath);
        }
        
        return JSON.stringify(analysis);
    } catch (error) {
        return JSON.stringify({ error: error.message });
    }
}

async function analyzeProjectFile(projectPath) {
    try {
        const content = await fs.readFile(projectPath, 'utf8');
        const projectInfo = {
            fileName: path.basename(projectPath),
            framework: extractFramework(content),
            projectReferences: extractProjectReferences(content),
            packageReferences: extractPackageReferences(content),
            outputType: extractOutputType(content)
        };
        
        return projectInfo;
    } catch (error) {
        return { error: `Failed to analyze project file: ${error.message}` };
    }
}

function extractFramework(content) {
    const frameworkMatch = content.match(/<TargetFramework[s]?>(.*?)<\/TargetFramework[s]?>/);
    return frameworkMatch ? frameworkMatch[1] : "Unknown";
}

function extractProjectReferences(content) {
    const references = [];
    const regex = /<ProjectReference\s+Include="([^"]*)"[^>]*>/g;
    let match;
    
    while ((match = regex.exec(content)) !== null) {
        references.push(match[1]);
    }
    
    return references;
}

function extractPackageReferences(content) {
    const packages = [];
    const regex = /<PackageReference\s+Include="([^"]*)"[^>]*Version="([^"]*)"[^>]*>/g;
    let match;
    
    while ((match = regex.exec(content)) !== null) {
        packages.push({ name: match[1], version: match[2] });
    }
    
    return packages;
}

function extractOutputType(content) {
    const outputMatch = content.match(/<OutputType>(.*?)<\/OutputType>/);
    return outputMatch ? outputMatch[1] : "Library";
}

async function analyzeProjectStructure(projectPath) {
    const projectDir = path.dirname(projectPath);
    const structure = {
        rootDirectory: projectDir,
        sourceFiles: [],
        folders: [],
        totalFiles: 0,
        codeMetrics: {}
    };
    
    try {
        await analyzeDirectory(projectDir, structure);
        structure.codeMetrics = calculateCodeMetrics(structure.sourceFiles);
        
        return structure;
    } catch (error) {
        return { error: `Failed to analyze structure: ${error.message}` };
    }
}

async function analyzeDirectory(dirPath, structure, relativePath = '') {
    try {
        const items = await fs.readdir(dirPath);
        
        for (const item of items) {
            const fullPath = path.join(dirPath, item);
            const stats = await fs.stat(fullPath);
            
            if (stats.isDirectory()) {
                if (!item.startsWith('.') && item !== 'bin' && item !== 'obj') {
                    structure.folders.push(path.join(relativePath, item));
                    await analyzeDirectory(fullPath, structure, path.join(relativePath, item));
                }
            } else {
                const ext = path.extname(item).toLowerCase();
                if (['.cs', '.js', '.ts', '.py', '.json', '.xml'].includes(ext)) {
                    structure.sourceFiles.push({
                        path: path.join(relativePath, item),
                        extension: ext,
                        size: stats.size,
                        modified: stats.mtime
                    });
                    structure.totalFiles++;
                }
            }
        }
    } catch (error) {
        console.error(`Error analyzing directory ${dirPath}: ${error.message}`);
    }
}

function calculateCodeMetrics(sourceFiles) {
    const metrics = {
        totalFiles: sourceFiles.length,
        filesByType: {},
        totalSize: 0,
        averageFileSize: 0
    };
    
    sourceFiles.forEach(file => {
        metrics.totalSize += file.size;
        metrics.filesByType[file.extension] = (metrics.filesByType[file.extension] || 0) + 1;
    });
    
    metrics.averageFileSize = metrics.totalFiles > 0 ? Math.round(metrics.totalSize / metrics.totalFiles) : 0;
    
    return metrics;
}

async function manageSolution(params) {
    const { solutionPath, operation, configuration = "Debug" } = params;
    
    const result = {
        solutionPath,
        operation,
        configuration,
        success: false,
        output: "",
        errors: [],
        duration: 0
    };
    
    const startTime = Date.now();
    
    try {
        switch (operation) {
            case 'build':
                result.output = await executeMSBuild(solutionPath, "Build", configuration);
                break;
            case 'clean':
                result.output = await executeMSBuild(solutionPath, "Clean", configuration);
                break;
            case 'restore':
                result.output = await executeCommand(`dotnet restore "${solutionPath}"`);
                break;
            case 'analyze':
                result.output = await analyzeSolutionStructure(solutionPath);
                break;
        }
        
        result.success = true;
        result.duration = Date.now() - startTime;
        
        return JSON.stringify(result);
    } catch (error) {
        result.errors.push(error.message);
        result.duration = Date.now() - startTime;
        return JSON.stringify(result);
    }
}

async function executeMSBuild(projectPath, target, configuration) {
    return new Promise((resolve, reject) => {
        const command = `msbuild "${projectPath}" /t:${target} /p:Configuration=${configuration} /v:minimal`;
        
        exec(command, (error, stdout, stderr) => {
            if (error) {
                reject(new Error(`MSBuild failed: ${stderr || error.message}`));
            } else {
                resolve(stdout);
            }
        });
    });
}

async function executeCommand(command) {
    return new Promise((resolve, reject) => {
        exec(command, (error, stdout, stderr) => {
            if (error) {
                reject(new Error(`Command failed: ${stderr || error.message}`));
            } else {
                resolve(stdout);
            }
        });
    });
}

async function handleNuGet(params) {
    const { projectPath, operation, packageName, version } = params;
    
    const result = {
        projectPath,
        operation,
        packageName,
        version,
        success: false,
        output: "",
        packages: []
    };
    
    try {
        switch (operation) {
            case 'list':
                result.packages = await listNuGetPackages(projectPath);
                break;
            case 'install':
                result.output = await installNuGetPackage(projectPath, packageName, version);
                break;
            case 'update':
                result.output = await updateNuGetPackage(projectPath, packageName);
                break;
            case 'remove':
                result.output = await removeNuGetPackage(projectPath, packageName);
                break;
            case 'search':
                result.packages = await searchNuGetPackages(packageName);
                break;
        }
        
        result.success = true;
        return JSON.stringify(result);
    } catch (error) {
        result.output = error.message;
        return JSON.stringify(result);
    }
}

async function listNuGetPackages(projectPath) {
    const command = `dotnet list "${projectPath}" package`;
    const output = await executeCommand(command);
    
    // Parse dotnet list output
    const packages = [];
    const lines = output.split('\n');
    
    for (const line of lines) {
        const match = line.match(/\s+>\s+([^\s]+)\s+([^\s]+)/);
        if (match) {
            packages.push({ name: match[1], version: match[2] });
        }
    }
    
    return packages;
}

async function installNuGetPackage(projectPath, packageName, version) {
    const versionPart = version ? ` --version ${version}` : '';
    const command = `dotnet add "${projectPath}" package ${packageName}${versionPart}`;
    return await executeCommand(command);
}

async function handleExtensionIntegration(params) {
    const { operation, context = {} } = params;
    
    const integration = {
        operation,
        context,
        extensionStatus: "Active",
        availableCommands: [],
        diagnostics: {}
    };
    
    switch (operation) {
        case 'status':
            integration.extensionStatus = "Running";
            integration.diagnostics = {
                agentsLoaded: 15,
                mcpServersConnected: 5,
                lastHealthCheck: new Date().toISOString()
            };
            break;
            
        case 'commands':
            integration.availableCommands = [
                "A3sist.AnalyzeCode",
                "A3sist.RefactorCode", 
                "A3sist.ValidateCode",
                "A3sist.GenerateTests",
                "A3sist.OptimizeImports"
            ];
            break;
            
        case 'settings':
            integration.settings = {
                enabledAgents: ["CSharp", "JavaScript", "Python"],
                mcpEnabled: true,
                autoAnalysis: true
            };
            break;
    }
    
    return JSON.stringify(integration);
}

async function handleLLMChat(req, res) {
    const { messages } = req.body.params;
    
    res.json({
        success: true,
        content: `Visual Studio Integration Server processed: ${messages[0]?.content?.substring(0, 100)}...`,
        actualProvider: "VSIntegrationMCP",
        actualModel: "vs-integration-1",
        tokensUsed: 100
    });
}

const PORT = process.env.VS_INTEGRATION_PORT || 3002;
app.listen(PORT, () => {
    console.log(`🚀 A3sist VS Integration MCP Server running on port ${PORT}`);
    console.log(`📚 Available tools: ${tools.map(t => t.name).join(', ')}`);
    console.log(`🔧 Specialized for: Visual Studio integration, project management`);
});

module.exports = app;