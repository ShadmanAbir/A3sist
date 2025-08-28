using Microsoft.AspNetCore.SignalR;
using A3sist.API.Models;

namespace A3sist.API.Hubs;

public class A3sistHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    // Methods for sending real-time updates
    public async Task SendChatMessage(ChatMessage message)
    {
        await Clients.All.SendAsync("ChatMessageReceived", message);
    }

    public async Task SendAgentProgress(AgentProgressEventArgs progress)
    {
        await Clients.All.SendAsync("AgentProgressChanged", progress);
    }

    public async Task SendAgentCompleted(AgentAnalysisCompletedEventArgs result)
    {
        await Clients.All.SendAsync("AgentAnalysisCompleted", result);
    }

    public async Task SendRAGProgress(IndexingProgressEventArgs progress)
    {
        await Clients.All.SendAsync("RAGIndexingProgress", progress);
    }

    public async Task SendMCPStatusChange(MCPServerStatusChangedEventArgs status)
    {
        await Clients.All.SendAsync("MCPServerStatusChanged", status);
    }

    public async Task SendModelChange(ModelChangedEventArgs modelChange)
    {
        await Clients.All.SendAsync("ActiveModelChanged", modelChange);
    }
}