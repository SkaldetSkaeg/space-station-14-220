namespace Content.Server.Forensics
{
    /// <summary>
    /// Used to take a sample of someone's fingerprints.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ForensicPadComponent : Component
    {
        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;

        public bool Used = false;
        public String Sample = string.Empty;

        // SS220 glove prints begin
        /// <summary>
        /// Identity captured with the sample, or null for samples that do not come from gloves.
        /// </summary>
        public string? GlovePrint;
        // SS220 glove prints end
    }
}
