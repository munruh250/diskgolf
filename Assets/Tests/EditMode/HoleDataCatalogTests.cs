using System;
using System.IO;
using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataCatalogTests
    {
        [Test]
        public void SaveThenLoad_PreservesNameAndTiles()
        {
            var dir = Path.Combine(Path.GetTempPath(), "diskg_catalog_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var catalog = new HoleDataCatalog(dir);
                var data = HoleDataTemplates.CreateStraightPar3("My Hole");
                catalog.Save(data, published: false);
                var entries = catalog.ListEntries();
                Assert.AreEqual(1, entries.Count);
                var loaded = catalog.Load(entries[0].Id);
                Assert.AreEqual("My Hole", loaded.Name);
                Assert.AreEqual(data.Hole.Tee, loaded.Hole.Tee);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Delete_MovesJsonAndMetaToTrash()
        {
            var dir = Path.Combine(Path.GetTempPath(), "diskg_catalog_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var catalog = new HoleDataCatalog(dir);
                var data = HoleDataTemplates.CreateStraightPar3("Trash Me");
                catalog.Save(data, published: true);
                string id = data.Id;

                catalog.Delete(id);

                Assert.AreEqual(0, catalog.ListEntries().Count);
                Assert.IsFalse(File.Exists(Path.Combine(dir, id + ".json")));
                Assert.IsFalse(File.Exists(Path.Combine(dir, id + ".meta.json")));
                Assert.IsTrue(File.Exists(Path.Combine(dir, ".trash", id + ".json")));
                Assert.IsTrue(File.Exists(Path.Combine(dir, ".trash", id + ".meta.json")));
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }
}
