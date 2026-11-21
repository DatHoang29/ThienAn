DiagnosticRegressionTests.Run();
await HubDisconnectRegressionTests.RunAllAsync();
Console.WriteLine("PASS: seven hub and cancellation checks");
await AuthRegression.RunAsync();
await ConnectionRegressionTests.RunAllAsync();
Console.WriteLine("PASS: all SSE regression checks");
