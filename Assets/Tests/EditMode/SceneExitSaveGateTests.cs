using System;
using System.Reflection;
using NUnit.Framework;

public class SceneExitSaveGateTests
{
    [Test]
    public void RepeatedLifecycleCallbacksFlushOnceUntilNextChange()
    {
        Type gateType = Type.GetType("SceneExitSaveGate, Assembly-CSharp");
        Assert.NotNull(gateType);
        object gate = Activator.CreateInstance(gateType, true);
        MethodInfo flush = gateType.GetMethod("Flush", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo noteChange = gateType.GetMethod("NoteChange", BindingFlags.Instance | BindingFlags.NonPublic);
        int writes = 0;
        Func<bool> captureAndFlush = () => { writes++; return true; };

        flush.Invoke(gate, new object[] { captureAndFlush });
        flush.Invoke(gate, new object[] { captureAndFlush });
        flush.Invoke(gate, new object[] { captureAndFlush });
        Assert.AreEqual(1, writes);

        noteChange.Invoke(gate, null);
        flush.Invoke(gate, new object[] { captureAndFlush });
        Assert.AreEqual(2, writes);
    }

    [Test]
    public void PendingWriteAllowsNextLifecycleCallbackToRetry()
    {
        Type gateType = Type.GetType("SceneExitSaveGate, Assembly-CSharp");
        Assert.NotNull(gateType);
        object gate = Activator.CreateInstance(gateType, true);
        MethodInfo flush = gateType.GetMethod("Flush", BindingFlags.Instance | BindingFlags.NonPublic);
        int attempts = 0;
        Func<bool> captureAndFlush = () => ++attempts > 1;

        flush.Invoke(gate, new object[] { captureAndFlush });
        flush.Invoke(gate, new object[] { captureAndFlush });
        flush.Invoke(gate, new object[] { captureAndFlush });
        Assert.AreEqual(2, attempts);
    }

    [Test]
    public void FailedFlushCanRetry()
    {
        Type gateType = Type.GetType("SceneExitSaveGate, Assembly-CSharp");
        Assert.NotNull(gateType);
        object gate = Activator.CreateInstance(gateType, true);
        MethodInfo flush = gateType.GetMethod("Flush", BindingFlags.Instance | BindingFlags.NonPublic);
        int attempts = 0;
        Func<bool> captureAndFlush = () =>
        {
            attempts++;
            if (attempts == 1)
                throw new InvalidOperationException("disk unavailable");
            return true;
        };

        Assert.Throws<TargetInvocationException>(() => flush.Invoke(gate, new object[] { captureAndFlush }));
        flush.Invoke(gate, new object[] { captureAndFlush });
        Assert.AreEqual(2, attempts);
    }
}