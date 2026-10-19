using System;
using System.Collections.Generic;
using System.IO;

internal sealed class AlbumResetService
{
    internal IReadOnlyList<Exception> Reset(CharacterData character, AlbumMediaStore store,
        Action save, Func<string> saveError)
    {
        var removed = new List<AlbumImageData>(character.albumImages);
        character.albumImages.Clear();
        try
        {
            save();
            string error = saveError();
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
        catch
        {
            character.albumImages.AddRange(removed);
            throw;
        }

        var cleanupErrors = new List<Exception>();
        foreach (AlbumImageData image in removed)
        {
            try { store.Delete(image); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                cleanupErrors.Add(error);
            }
        }
        return cleanupErrors;
    }
}
