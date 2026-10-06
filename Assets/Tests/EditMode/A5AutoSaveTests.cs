using System;
using System.IO;
using NUnit.Framework;

public class A5AutoSaveTests
{
    [Test]
    public void TenRapidChangesProduceOneDiskWriteAndExplicitFlushDoesNotDuplicateIt()
    {
        string directory = Path.Combine(Path.GetTempPath(), "TaruckA5_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string savePath = Path.Combine(directory, "DndCharactersData.taruck-data");
            var data = new AppSaveData();
            var gate = new AutoSaveWriteGate();
            int diskWrites = 0;

            for (int i = 0; i < 10; i++)
                gate.Request(i * 0.1f);

            Assert.That(gate.IsDue(1.69f, 0.8f), Is.False);
            Assert.That(diskWrites, Is.Zero);
            Assert.That(File.Exists(savePath), Is.False);

            Assert.That(gate.IsDue(1.71f, 0.8f), Is.True);
            TaruckLocalRepository.Save(savePath, data, "A5-test");
            diskWrites++;
            gate.MarkSaved();

            Assert.That(diskWrites, Is.EqualTo(1));
            Assert.That(File.Exists(savePath), Is.True);
            Assert.That(gate.IsDue(3f, 0.8f), Is.False, "No second write after a successful flush");

            for (int i = 0; i < 10; i++)
                gate.Request(3f + i * 0.1f);
            Assert.That(gate.Pending, Is.True);
            TaruckLocalRepository.Save(savePath, data, "A5-test");
            diskWrites++;
            gate.MarkSaved();
            Assert.That(diskWrites, Is.EqualTo(2));
            Assert.That(File.Exists(savePath + ".bak"), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
