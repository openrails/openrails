using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.IO;

namespace GNU.Gettext.Msgfmt
{
    public class ResourcesGen
    {
		public Options Options { get; private set; }

        public ResourcesGen(Options options)
        {
			this.Options = options;
        }

        public void Run()
        {
			Catalog catalog = new Catalog();
			foreach(string fileName in Options.InputFiles)
			{
				Catalog temp = new Catalog();
				temp.Load(fileName);
				catalog.Append(temp);
			}

            using (ResourceWriter writer = new ResourceWriter(Options.OutFile))
            {
                foreach (CatalogEntry entry in catalog)
                {
                    try
					{
						writer.AddResource(entry.Key, entry.IsTranslated ? entry.GetTranslation(0) : entry.String);
					}
                    catch (Exception e)
					{
						string message = String.Format("Error adding item {0}", entry.String);
						if (!String.IsNullOrEmpty(entry.Context))
							message = String.Format("Error adding item {0} in context '{1}'",
							                        entry.String, entry.Context);
						throw new Exception(message, e);
					}
                }
                writer.Generate();
            }
        }
    }
}
