using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NLog;
using STak.TakEngine;

namespace STak.TakHub.Client
{
    // TODO - Parameterize whether to rethrow exceptions?
    // TODO - Or have mediator raise an error event?

    public class HubCallMediator
    {
        private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();


        public static void InvokeCommand(TakCommand command, Action action)
        {
            try
            {
                LogTakHubCommand(command);
                action();
            }
            catch (Exception ex)
            {
                LogTakHubCommandException(command, ex);
            }
        }


        public static async Task<T> InvokeCommandAsync<T>(TakCommand command, Func<Task<T>> action)
        {
            T result;

            try
            {
                LogTakHubCommand(command);
                result = await action();
                LogTakHubCommandResult(command, result);
            }
            catch (Exception ex)
            {
                LogTakHubCommandException(command, ex);
                result = default;
            }

            return result;
        }


        public static async Task InvokeCommandAsync(TakCommand command, Func<Task> action)
        {
            try
            {
                LogTakHubCommand(command);
                await action();
            }
            catch (Exception ex)
            {
                LogTakHubCommandException(command, ex);
            }
        }


        public static void ProcessNotification(TakNotification notification, Action action)
        {
            try
            {
                LogTakHubNotification(notification);
                action();
            }
            catch (Exception ex)
            {
                LogTakHubNotificationException(notification, ex);
            }
        }


        public static async Task ProcessNotificationAsync(TakNotification notification, Func<Task> action)
        {
            try
            {
                LogTakHubNotification(notification);
                await action();
            }
            catch (Exception ex)
            {
                LogTakHubNotificationException(notification, ex);
            }
        }


        private static void LogTakHubCommand(TakCommand command)
        {
            if (command != GameCommand.TrackMove)
            {
                s_logger.Debug($"Invoking hub command \"{command.Name}\".");
            }
        }


        private static void LogTakHubCommandResult(TakCommand command, object result)
        {
            if (command != GameCommand.TrackMove)
            {
                s_logger.Debug($"Hub command {command.Name} returned {result?.ToString()}");
            }
        }


        private static void LogTakHubCommandException(TakCommand command, Exception ex)
        {
            s_logger.Debug(ex, $"Exception thrown while executing {command.Name}: {ex}");
        }


        private static void LogTakHubNotification(TakNotification notification)
        {
            if (notification != GameNotification.MoveTracked)
            {
                s_logger.Debug($"Processing hub notification \"{notification.Name}\".");
            }
        }


        private static void LogTakHubNotificationException(TakNotification notification, Exception ex)
        {
            s_logger.Debug(ex, $"Exception occurred while processing notification {notification.Name}: {ex}");
        }
    }
}
