namespace Watermelon
{
    /// <summary>
    /// Configuration object passed to <see cref="ISaveWrapper.Configure"/> during initialization.
    /// </summary>
    public class SaveWrapperConfig
    {
        /// <summary>Prefix used to namespace keys in WebGL localStorage, preventing collisions between different builds.</summary>
        public string WebGLPrefix { get; set; }
    }
}
