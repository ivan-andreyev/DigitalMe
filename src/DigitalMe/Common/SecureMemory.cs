using System.Runtime.InteropServices;

namespace DigitalMe.Common;

/// <summary>
/// Provides secure memory handling for sensitive data with automatic zeroing and memory pinning.
/// Implements IDisposable pattern to ensure memory cleanup.
/// Features:
/// - Memory pinning via GCHandle to prevent GC relocation
/// - Automatic zeroing of memory on disposal
/// - Safe for multiple Dispose calls
/// - Throws ObjectDisposedException if accessed after disposal
/// </summary>
public sealed class SecureMemory : IDisposable
{
    private byte[] data;
    private GCHandle handle;
    private bool disposed;

    /// <summary>
    /// Gets the protected data array.
    /// </summary>
    /// <exception cref="ObjectDisposedException">If accessed after disposal</exception>
    public byte[] Data
    {
        get
        {
            ThrowIfDisposed();
            return data;
        }
    }

    /// <summary>
    /// Initializes a new instance of SecureMemory with the specified data.
    /// The data will be pinned in memory to prevent GC relocation.
    /// </summary>
    /// <param name="data">The sensitive data to protect</param>
    /// <exception cref="ArgumentNullException">If data is null</exception>
    public SecureMemory(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        this.data = data;

        // Pin the memory to prevent GC from moving it
        handle = GCHandle.Alloc(this.data, GCHandleType.Pinned);
    }

    /// <summary>
    /// Disposes the SecureMemory instance, zeroing out memory and releasing the GC handle.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        try
        {
            // Always clear sensitive data (managed resource with security implications)
            if (data != null)
            {
                Array.Clear(data, 0, data.Length);
            }

            // Always free GCHandle (unmanaged resource)
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
        finally
        {
            disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure cleanup even if Dispose is not called.
    /// </summary>
    ~SecureMemory()
    {
        Dispose(false);
    }

    /// <summary>
    /// Throws ObjectDisposedException if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SecureMemory));
        }
    }
}