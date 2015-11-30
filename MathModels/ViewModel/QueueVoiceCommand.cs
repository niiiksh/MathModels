namespace MathModels.ViewModel
{
    /// <summary>
    /// Simple struct to pass voice-command arguments to the appropriate view, in order to execute the 
    /// action requested by the user. Reference OnActivated() in App.xaml.cs, and AdventureWorksCommands.xml
    /// for context on what should go in here.
    /// </summary>
    public struct QueueVoiceCommand
    {
        public string voiceCommand;
        public string commandMode;
        public string textSpoken;
        public string modelNumber;

        /// <summary>
        /// Set up the voice command struct with the provided details about the voice command.
        /// Oriented around the "showGraph" VCD command (See vcd.xml)
        /// </summary>
        /// <param name="voiceCommand">The voice command (the Command element in the VCD xml) </param>
        /// <param name="commandMode">The command mode (whether it was voice or text activation)</param>
        /// <param name="textSpoken">The raw voice command text.</param>
        /// <param name="modelNumber">The model number parameter.</param>
        public QueueVoiceCommand(string voiceCommand, string commandMode, string textSpoken, string modelNumber)
        {
            this.voiceCommand = voiceCommand;
            this.commandMode = commandMode;
            this.textSpoken = textSpoken;
            this.modelNumber = modelNumber;
        }
    }
}
