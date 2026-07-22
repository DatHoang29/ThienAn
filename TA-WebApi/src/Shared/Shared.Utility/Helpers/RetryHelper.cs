namespace Shared.Utility.Helpers
{
    public class RetryHelper
    {

        public static void RetryOnException(int retryAttempts, TimeSpan delay, Action operation)
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    attempts++;
                    operation();
                    break; // Sucess! Lets exit the loop!
                }
                catch (Exception ex)
                {
                    if (attempts == retryAttempts) throw;
                    Task.Delay(delay).Wait();
                }
            }
        }

        public static T RetryOnException<T>(int retryAttempts, TimeSpan delay, Func<T> operation)
        {
            T result = default(T);
            var attempts = 0;
            while (true)
            {
                try
                {
                    attempts++;
                    result = operation();
                    break; // Sucess! Lets exit the loop!
                }
                catch (Exception ex)
                {
                    if (attempts == retryAttempts) throw;
                    Task.Delay(delay).Wait();
                    result = default(T);
                }
            }
            return result;
        }
    }
}
