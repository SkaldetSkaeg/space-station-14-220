using Content.Shared.SS220.Forensics; // SS220 glove prints
using Content.Server.SS220.Forensics.Serialization; // SS220 material profiles

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed partial class ForensicsComponent : Component
    {
        [DataField("fingerprints")]
        public HashSet<string> Fingerprints = new();

        [DataField("fibers")]
        public HashSet<string> Fibers = new();

        // SS220 glove prints begin
        /// <summary>
        /// Identities and types of gloves that touched this entity, independent of fiber descriptions.
        /// </summary>
        [DataField]
        public HashSet<GlovePrintTrace> GlovePrints = [];
        // SS220 glove prints end

        //SS220 Micro_fibers start
        /// <summary>
        /// Individual material profiles left by clothing and storage on this entity.
        /// </summary>
        [DataField("microFibers", customTypeSerializer: typeof(MicroFiberTraceSetSerializer))]
        public HashSet<MicroFiberTrace> MicroFibers = [];
        //SS220 Micro_fibers end
        [DataField("dnas")]
        public HashSet<string> DNAs = new();

        [DataField("residues")]
        public HashSet<string> Residues = new();

        /// <summary>
        /// How close you must be to wipe the prints/blood/etc. off of this entity
        /// </summary>
        [DataField("cleanDistance")]
        public float CleanDistance = 1.5f;

        /// <summary>
        /// Can the DNA be cleaned off of this entity?
        /// e.g. you can wipe the DNA off of a knife, but not a cigarette
        /// </summary>
        [DataField("canDnaBeCleaned")]
        public bool CanDnaBeCleaned = true;
    }
}
