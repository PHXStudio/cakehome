using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Experience")]
    public class ExperienceInitModule : InitModule
    {
        public override string ModuleName => "Experience";

        [SerializeField] ExperienceDatabase levelDatabase;

        private ExperienceController experienceController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            experienceController = new ExperienceController(levelDatabase);
            yield break;
        }

        public override void Unload()
        {
            experienceController?.Unload();
        }
    }
}
