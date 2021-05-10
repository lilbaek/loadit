namespace loadit.shared.Ipc
{
    public interface IClientCommunication
    {
        /// <summary>
        /// Gets a configuration value from the test project
        /// </summary>
        string GetConfiguration(string key);
        
        /// <summary>
        /// Gets a configuration value from a section from the test project
        /// </summary>
        string GetConfigurationSection(string section, string key);
        
        /// <summary>
        /// Tell the client we can start testing. (CLI is ready)
        /// </summary>
        void StartTesting();
        
        /// <summary>
        /// Are we allowed to start testing
        /// </summary>
        /// <returns></returns>
        bool CanStart();
    }
}