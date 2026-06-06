namespace ExpensesBackend.API.Domain.DTOs;

public record PushSubscribeRequest(string Endpoint, string P256dh, string Auth);

public record PushUnsubscribeRequest(string Endpoint);
