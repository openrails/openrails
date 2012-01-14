using System;
using System.Collections.Generic;
namespace ORArchitecture
{
    /// <summary>
    /// Represents OpenRail's entire file system.  Provides access to all data stored on 
    /// the user's hard disk - ie shapes, images, activity files, world files etc
    /// </summary>
    interface IContentData
    {
        /// <summary>
        /// Retrieve the specified item.
        /// </summary>
        Object this[ContentID id] { get; }

        /// <summary>
        /// Retrieve a list of ID's for all the items of the specified type.
        /// </summary>
        /// <param name="type">Represents a class type that is stored in the database, ie Shape</param>
        /// <returns></returns>
        List<ContentID> this[Type type] { get; } 
    }

    /// <summary>
    /// Defines access to a specific data item ( ie file )
    /// When one file references another, it will do so via an ID
    /// The implementation of an ID depends on how we ultimately store our data on disk.
    /// It might be a file path, or a PackageName, FileName pair, but the implementation
    /// method is irrelevant to the users of the ContentDatabase since the users
    /// of the ContentDatabase will always be handed a link as an abstract ID.
    /// </summary>
    abstract class ContentID
    {
        //public string PackageName;
        //public string FileName;
    }

/*        
    /// <summary>
    /// USE CASE EXAMPLE
    /// </summary>
    public class UseCaseExamples
    {
        IContentData Data;

        public void ListAllLocomotives()
        {
            foreach ( ID locoID in Data[typeof(Locomotive)] )
            {
                var locomotive = (Locomotive)Data[locoID];
                Console.WriteLine(locomotive.Name, locomotive.LengthM);
            }
        }

        public void ListImagesUsedInLocomotive(ContentID locomotiveID)
        {
            var locomotive = (Locomotive)Data[locomotiveID];
            var shape = (Shape)Data[locomotive.ShapeID];
            foreach (var imageID in shape.ImageIDs)
            {
                var image = (Image)Data[imageID];
                // do stuff with the image
            }
        }
    }// UseCaseExamples
*/
}
