// KeyJaya: Compatibility handling for an older installation's Espanso startup service.
// This is setup-only migration support, not a dependency of the resident conversion engine.
using System;

namespace KeyJaya
{
    public sealed class ServiceResult
    {
        public readonly int Code;
        public readonly string Detail;
        public ServiceResult(int code, string detail = "")
        {
            Code = code;
            Detail = detail;
        }
    }

    // Espanso service check: 0 registered, 2 absent. Status: 0 running, 4 stopped.
    // An unsuccessful command is never evidence that the service has been disabled.
    public sealed class EspansoMigration
    {
        readonly Func<string, ServiceResult> command;
        bool inspected, registered, running, restoreRegistration, restoreRunning;
        public EspansoMigration(Func<string, ServiceResult> command)
        {
            this.command = command;
        }

        static bool State(ServiceResult result, int absent, string action)
        {
            if (result.Code == 0)
                return true;
            if (result.Code == absent)
                return false;
            throw Failure(action, result);
        }

        static Exception Failure(string action, ServiceResult result)
        {
            return new InvalidOperationException("Espanso " + action + " failed (exit " + result.Code + "). " + result.Detail);
        }

        public void Inspect()
        {
            registered = State(command("service check"), 2, "startup check");
            running = State(command("service status"), 4, "running-state check");
            inspected = true;
        }

        public void Disable()
        {
            if (!inspected)
                throw new InvalidOperationException("Espanso state was not inspected.");
            if (running)
            {
                restoreRunning = true;
                ServiceResult stop = command("service stop");
                if (State(command("service status"), 4, "stop verification"))
                    throw Failure("stop", stop);
            }

            if (registered)
            {
                restoreRegistration = true;
                ServiceResult unregister = command("service unregister");
                if (State(command("service check"), 2, "unregister verification"))
                    throw Failure("startup unregister", unregister);
            }
        }

        public void Rollback()
        {
            if (restoreRegistration && !State(command("service check"), 2, "rollback startup check"))
            {
                ServiceResult result = command("service register");
                if (!State(command("service check"), 2, "rollback startup verification"))
                    throw Failure("startup restoration", result);
            }

            if (restoreRunning && !State(command("service status"), 4, "rollback running-state check"))
            {
                ServiceResult result = command("service start");
                if (!State(command("service status"), 4, "rollback running-state verification"))
                    throw Failure("running-state restoration", result);
            }
        }
    }
}
