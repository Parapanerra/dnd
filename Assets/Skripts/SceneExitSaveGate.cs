using System;

// Coalesces repeated pause/disable/quit callbacks until the scene changes again.
internal sealed class SceneExitSaveGate
{
    private bool flushed;

    internal void NoteChange()
    {
        flushed = false;
    }

    internal void Flush(Func<bool> captureAndFlush)
    {
        if (flushed || captureAndFlush == null)
            return;

        flushed = captureAndFlush();
    }
}