const express = require('express');
const cors = require('cors');
const { exec } = require('child_process');

const app = express();
app.use(cors());
app.use(express.json());

const tools = [
    {
        name: "git_operations",
        description: "Execute Git operations and analyze repository",
        parameters: {
            type: "object",
            properties: {
                operation: { type: "string", enum: ["status", "log", "diff", "branch", "commit-analysis"] },
                repositoryPath: { type: "string" },
                options: { type: "object" }
            }
        }
    },
    {
        name: "ci_cd_integration",
        description: "Integrate with CI/CD pipelines",
        parameters: {
            type: "object",
            properties: {
                platform: { type: "string", enum: ["azure-devops", "github-actions", "jenkins"] },
                operation: { type: "string" },
                config: { type: "object" }
            }
        }
    },
    {
        name: "deployment_analysis",
        description: "Analyze deployment readiness and requirements",
        parameters: {
            type: "object",
            properties: {
                projectPath: { type: "string" },
                targetEnvironment: { type: "string" },
                deploymentType: { type: "string" }
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
        case 'git_operations':
            result = await handleGitOperations(parameters);
            break;
        case 'ci_cd_integration':
            result = await handleCICD(parameters);
            break;
        case 'deployment_analysis':
            result = await analyzeDeployment(parameters);
            break;
        default:
            return res.status(400).json({ success: false, error: `Unknown tool: ${name}` });
    }
    
    res.json({
        success: true,
        result,
        metadata: { toolName: name, serverType: "GitDevOps" }
    });
}

async function handleGitOperations(params) {
    const { operation, repositoryPath, options = {} } = params;
    
    const gitResult = {
        operation,
        repositoryPath,
        output: "",
        branch: "main",
        commits: [],
        changes: []
    };

    try {
        switch (operation) {
            case 'status':
                gitResult.output = await executeCommand(`git status --porcelain`, repositoryPath);
                break;
            case 'log':
                const logOutput = await executeCommand(`git log --oneline -10`, repositoryPath);
                gitResult.commits = logOutput.split('\n').filter(line => line.trim());
                break;
            case 'diff':
                gitResult.output = await executeCommand(`git diff --stat`, repositoryPath);
                break;
            case 'branch':
                gitResult.output = await executeCommand(`git branch -a`, repositoryPath);
                break;
            case 'commit-analysis':
                gitResult.commits = await analyzeRecentCommits(repositoryPath);
                break;
        }
        
        return JSON.stringify(gitResult);
    } catch (error) {
        gitResult.error = error.message;
        return JSON.stringify(gitResult);
    }
}

async function executeCommand(command, workingDir = process.cwd()) {
    return new Promise((resolve, reject) => {
        exec(command, { cwd: workingDir }, (error, stdout, stderr) => {
            if (error) {
                reject(new Error(stderr || error.message));
            } else {
                resolve(stdout.trim());
            }
        });
    });
}

async function analyzeRecentCommits(repositoryPath) {
    try {
        const output = await executeCommand(`git log --pretty=format:"%h|%an|%ad|%s" --date=short -20`, repositoryPath);
        return output.split('\n').map(line => {
            const [hash, author, date, message] = line.split('|');
            return { hash, author, date, message };
        });
    } catch (error) {
        return [];
    }
}

async function handleCICD(params) {
    const { platform, operation, config = {} } = params;
    
    const cicdResult = {
        platform,
        operation,
        config,
        pipelines: [],
        status: "success"
    };

    // Mock CI/CD operations
    switch (platform) {
        case 'azure-devops':
            cicdResult.pipelines = ["Build", "Test", "Deploy"];
            break;
        case 'github-actions':
            cicdResult.pipelines = [".github/workflows/ci.yml", ".github/workflows/deploy.yml"];
            break;
        case 'jenkins':
            cicdResult.pipelines = ["Jenkinsfile"];
            break;
    }
    
    return JSON.stringify(cicdResult);
}

async function analyzeDeployment(params) {
    const { projectPath, targetEnvironment, deploymentType } = params;
    
    const analysis = {
        projectPath,
        targetEnvironment,
        deploymentType,
        readiness: "green",
        requirements: [],
        recommendations: [
            "Ensure all dependencies are included",
            "Verify environment configuration",
            "Run full test suite before deployment"
        ]
    };
    
    return JSON.stringify(analysis);
}

const PORT = process.env.GIT_DEVOPS_PORT || 3004;
app.listen(PORT, () => {
    console.log(`🚀 A3sist Git & DevOps MCP Server running on port ${PORT}`);
});