﻿using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    
    [TestFixture]
    public class DocumentHolderTests : IntegrationTestBase
    {
        public class MappedDocument : DocumentHolder
        {
            public string Name
            {
                get { return Get("Name"); }
                set { Set("Name", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public string Id
            {
                get { return Get("Id"); }
                set { Set("Id", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public int? Scalar
            {
                get { return GetNumeric<int>("Scalar"); }
                set { SetNumeric("Scalar", value); }
            }
        }

        protected override Analyzer GetAnalyzer(Util.Version version)
        {
            var a = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            //a.AddAnalyzer(new NumberAnalyzer());
            return a;
        }

        [Test]
        public void Select()
        {
            var d = new MappedDocument {Name = "My Document"};
            AddDocument(d.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents select doc;

            Assert.That(result.FirstOrDefault().Name, Is.EqualTo(d.Name));
        }

        [Test]
        public void SelectScalar()
        {
            const int scalar = 99;

            var d = new MappedDocument {Name = "a", Scalar = scalar};

            AddDocument(d.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents select doc.Scalar;

            Assert.That(result.FirstOrDefault(), Is.EqualTo(scalar));
        }

        [Test]
        public void Where()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12}.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name == "My" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id == "X.Z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch_CaseInsensitive()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch_Phrase()
        {
            AddDocument(new MappedDocument { Name = "Documents Bill", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "Bills Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name == "\"Bills Document\"" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void Where_NotAnalyzed_StartsWith()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id.StartsWith("x.z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_NotAnalyzed_CaseInsensitive()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id.StartsWith("x.z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_StartsWith()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name.StartsWith("my") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_NotEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name != "\"My Document\"" select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_NotNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = null, Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name != null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_Null()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = null, Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name == null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == 12 select doc;

            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarNotEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 11}.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar != 11 select doc;

            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }


        [Test]
        public void Where_ScalarNotNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Scalar != null select doc).ToList();

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ScalarNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == null select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

    }
 
}