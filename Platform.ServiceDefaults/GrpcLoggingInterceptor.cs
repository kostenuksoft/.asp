using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        using IDisposable? scope = logger.BeginScope(
            new Dictionary<string, object> { ["GrpcMethod"] = context.Method }
        );
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            TResponse response = await continuation(request, context);
            logger.LogInformation(
                "gRPC {GrpcMethod} from {Peer} completed with {GrpcStatus} in {ElapsedMilliseconds} ms",
                context.Method,
                context.Peer,
                StatusCode.OK,
                stopwatch.ElapsedMilliseconds
            );
            return response;
        }
        catch (RpcException exception)
        {
            logger.LogWarning(
                exception,
                "gRPC {GrpcMethod} from {Peer} failed with {GrpcStatus} in {ElapsedMilliseconds} ms",
                context.Method,
                context.Peer,
                exception.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
        catch (Exception exception) when (LogUnexpected(context, stopwatch, exception))
        {
            throw;
        }
    }

    private bool LogUnexpected(
        ServerCallContext context,
        Stopwatch stopwatch,
        Exception exception
    )
    {
        logger.LogError(
            exception,
            "gRPC {GrpcMethod} from {Peer} failed with {GrpcStatus} in {ElapsedMilliseconds} ms",
            context.Method,
            context.Peer,
            StatusCode.Internal,
            stopwatch.ElapsedMilliseconds
        );
        return false;
    }
}
