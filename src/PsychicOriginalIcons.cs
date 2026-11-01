using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CallOfTheWild.Wrath.Prototype
{
    internal static class PsychicOriginalIcons
    {
        private static readonly Dictionary<string, Sprite> Icons=new Dictionary<string, Sprite>();
        internal static Sprite Load(string token)
        {
            if(Icons.TryGetValue(token,out var icon))return icon;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CotW."+token+".png"))
            using(var bytes=new MemoryStream()) {
                if(stream==null)throw new InvalidOperationException("Missing original CotW icon: "+token);
                stream.CopyTo(bytes);var texture=new Texture2D(2,2);
                if(!ImageConversion.LoadImage(texture,bytes.ToArray()))throw new InvalidOperationException("Invalid original CotW icon: "+token);
                icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
                Icons.Add(token,icon);return icon;
            }
        }
    }
}
