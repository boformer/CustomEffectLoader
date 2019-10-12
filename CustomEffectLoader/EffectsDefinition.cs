using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace CustomEffectLoader
{
    public class EffectsDefinition
    {
        [XmlArray("Effects")]
        [XmlArrayItem("LightEffect", typeof(LightEffect))]
        public List<Effect> Effects { get; set; }

        public abstract class Effect
        {
            [XmlAttribute("name"), DefaultValue(null)]
            public string Name { get; set; }
        }

        public class LightEffect : Effect
        {
            [XmlAttribute("type"), DefaultValue("Spot")]
            public string Type { get; set; }

            [XmlAttribute("intensity"), DefaultValue(5f)]
            public float Intensity { get; set; }

            [XmlAttribute("range"), DefaultValue(30f)]
            public float Range { get; set; }

            [XmlAttribute("spotAngle"), DefaultValue(150f)]
            public float SpotAngle { get; set; }

            [XmlAttribute("spotLeaking"), DefaultValue(0.5f)]
            public float SpotLeaking { get; set; }

            [XmlAttribute("fadeStartDistance"), DefaultValue(300f)]
            public float FadeStartDistance { get; set; }

            [XmlAttribute("fadeEndDistance"), DefaultValue(500f)]
            public float FadeEndDistance { get; set; }

            [XmlAttribute("batchedLight"), DefaultValue(false)]
            public bool BatchedLight { get; set; }

            [XmlAttribute("offMin"), DefaultValue(0.4f)]
            public float OffMin { get; set; }

            [XmlAttribute("offMax"), DefaultValue(0.7f)]
            public float OffMax { get; set; }

            [XmlAttribute("renderDuration"), DefaultValue(0f)]
            public float RenderDuration { get; set; }

            [XmlAttribute("blinkType"), DefaultValue("None")]
            public string BlinkType { get; set; }

            [XmlAttribute("rotationSpeed"), DefaultValue(0)]
            public int RotationSpeed { get; set; }

            [XmlAttribute("rotationAxisX"), DefaultValue(0.0)]
            public float RotationAxisX { get; set; }

            [XmlAttribute("rotationAxisY"), DefaultValue(1.0)]
            public float RotationAxisY { get; set; }

            [XmlAttribute("rotationAxisZ"), DefaultValue(0.0)]
            public float RotationAxisZ { get; set; }

            public List<Color> VariationColors { get; set; } = new List<Color>();
        }

        public class Color
        {
            [XmlAttribute("r"), DefaultValue(255)]
            public byte R { get; set; }

            [XmlAttribute("g"), DefaultValue(255)]
            public byte G { get; set; }

            [XmlAttribute("b"), DefaultValue(255)]
            public byte B { get; set; }

            [XmlAttribute("A"), DefaultValue(255)]
            public byte A { get; set; }

            public UnityEngine.Color ToUnityColor()
            {
                return new UnityEngine.Color32(R, G, B, A);
        }
        }
    }
}
