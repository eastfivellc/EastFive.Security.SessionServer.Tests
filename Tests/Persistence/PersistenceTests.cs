using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EastFive.Api.Tests;
using EastFive.Extensions;
using EastFive;
using EastFive.Collections.Generic;
using EastFive.Api;

using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.Serialization;

using BlackBarLabs.Api.Resources;
using BlackBarLabs.Extensions;
using EastFive.Api.Controllers;
using EastFive.Azure.Persistence.AzureStorageTables;
using EastFive.Linq;
using EastFive.Linq.Async;
using Newtonsoft.Json;
using EastFive.Persistence;
using EastFive.Persistence.Azure.StorageTables;

namespace EastFive.Azure.Tests.Persistence
{


    [DataContract]
    public struct ComplexStorageModel : IReferenceable
    {
        public Guid id => resourceRef.id;

        public const string ResourceIdPropertyName = "id";
        [RowKey]
        public IRef<ComplexStorageModel> resourceRef;

        [DataContract]
        public class EmbeddedModel
        {
            #region Singles

            [Storage]
            public Guid guid;

            [Storage]
            public string stringProperty;

            [Storage]
            public object objectInt;

            [Storage]
            public object objectString;

            [Storage]
            public IRef<RelatedModel> relatedRef;

            [Storage]
            public IRefOptional<RelatedModel> relatedOptionalRef;

            #endregion

            #region Arrays

            [Storage]
            public Guid[] arrayGuid;

            [Storage]
            public string[] arrayString;

            [Storage]
            public object[] arrayObjectInt;

            [Storage]
            public object[] arrayObjectString;

            [Storage]
            public IRef<RelatedModel>[] arrayRef;

            [Storage]
            public IRef<IReferenceable>[] arrayRefObjObj;

            [Storage]
            public IRefOptional<RelatedModel>[] arrayRelatedOptionalRef;

            [Storage]
            public IRefOptional<IReferenceable>[] arrayRelatedOptionalObjObjRef;

            #endregion
        }

        public enum ExampleEnum
        {
            ascending,
            desending,
            neutral,
        }

        #region Singles

        public const string GuidPropertyName = "guid";
        [Storage(Name = GuidPropertyName)]
        public Guid guid;

        public const string StringPropertyName = "string";
        [Storage(Name = StringPropertyName)]
        public string stringProperty;

        [Storage]
        public object objectInt;

        [Storage]
        public object objectString;

        public const string RealtedPropertyName = "related";
        [Storage(Name = RealtedPropertyName)]
        public IRef<RelatedModel> relatedRef;

        [Storage]
        public IRef<IReferenceable> refObjObj;

        [Storage]
        public IRefOptional<RelatedModel> relatedOptionalRef;

        [Storage]
        public IRefOptional<IReferenceable> relatedOptionalObjObjRef;

        [Storage]
        public EmbeddedModel embeddedModel;

        #endregion

        #region Arrays

        [Storage]
        public Guid[] arrayGuid;

        [Storage]
        public string[] arrayString;

        [Storage]
        public object[] arrayObjectInt;

        [Storage]
        public object[] arrayObjectString;

        [Storage]
        public ExampleEnum[] arrayEnum;

        [Storage]
        public IRef<RelatedModel>[] arrayRef;

        [Storage]
        public IRef<IReferenceable>[] arrayRefObjObj;

        [Storage]
        public IRef<RelatedModelObj>[] arrayRefObj;

        [Storage]
        public IRefOptional<RelatedModel>[] arrayRelatedOptionalRef;

        [Storage]
        public IRefOptional<IReferenceable>[] arrayRelatedOptionalObjObjRef;

        [Storage]
        public EmbeddedModel[] arrayEmbeddedModel;

        #endregion

        #region Dictionary

        [Storage]
        public IDictionary<Guid, Guid[]> dictGuid;

        [Storage]
        public IDictionary<object, object[]> dictObject;

        [Storage]
        public IDictionary<string, string[]> dictObjectIntArrayEmbedArray;

        [Storage]
        public IDictionary<IRef<RelatedModel>, IRef<RelatedModel>[]> dictRef;

        [Storage]
        public IDictionary<IRef<IReferenceable>, IRef<IReferenceable>[]> dictRefObjectObj;

        [Storage]
        public IDictionary<IRefOptional<RelatedModel>, IRefOptional<RelatedModel>[]> dictRefOptional;

        [Storage]
        public IDictionary<IRefOptional<IReferenceable>, IRefOptional<IReferenceable>[]> dictRefOptionalObjObj;

        [Storage]
        public IDictionary<EmbeddedModel, EmbeddedModel[]> dictEmbeddedModel;

        #endregion

    }

    [DataContract]
    public struct RelatedModel : IReferenceable
    {
        public Guid id { get; set; }
    }

    [DataContract]
    public class RelatedModelObj : IReferenceable
    {
        public Guid id { get; set; }
    }

    [TestClass]
    public class ResourceTests
    {
        [TestMethod]
        public async Task DataStoresCorrectly()
        {
            var resourceRef = Guid.NewGuid().AsRef<ComplexStorageModel>();

            var embedded1 = new ComplexStorageModel.EmbeddedModel
            {
                guid = Guid.NewGuid(),
                objectInt = 3,
                objectString = "Barf",
                relatedOptionalRef = Guid.NewGuid().AsRef<RelatedModel>().Optional(),
                relatedRef = Guid.NewGuid().AsRef<RelatedModel>(),
                stringProperty = "Food",
                arrayGuid = new[] { Guid.NewGuid(), Guid.NewGuid() },
                arrayObjectInt = new object[] { 1, 2 },
                arrayObjectString = new object[] { null, "barF", string.Empty, "fooD" },
                arrayRef = new[] { Guid.NewGuid().AsRef<RelatedModel>(), Guid.NewGuid().AsRef<RelatedModel>() },
                arrayRefObjObj = new[] { Guid.NewGuid().AsRef<IReferenceable>(), Guid.NewGuid().AsRef<IReferenceable>() },
                arrayRelatedOptionalObjObjRef = new []
                {
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                    default(Guid?).AsRefOptional<IReferenceable>(),
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                },
                arrayRelatedOptionalRef = new[]
                {
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                    default(Guid?).AsRefOptional<RelatedModel>(),
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                },
                arrayString = new[] { "BARRF", null, string.Empty, "food", },
            };
            var embedded2 = new ComplexStorageModel.EmbeddedModel
            {
                guid = Guid.NewGuid(),
                objectInt = 4,
                objectString = "barf",
                relatedOptionalRef = Guid.NewGuid().AsRefOptional<RelatedModel>(),
                relatedRef = Guid.NewGuid().AsRef<RelatedModel>(),
                stringProperty = "food",
                arrayGuid = new[] { Guid.NewGuid(), Guid.NewGuid() },
                arrayObjectInt = new object[] { 1, 2 },
                arrayObjectString = new object[] { "food", string.Empty, null, "bar" },
                arrayRef = new[] { Guid.NewGuid().AsRef<RelatedModel>(), Guid.NewGuid().AsRef<RelatedModel>() },
                arrayRefObjObj = new[] { Guid.NewGuid().AsRef<IReferenceable>(), Guid.NewGuid().AsRef<IReferenceable>() },
                arrayRelatedOptionalObjObjRef = new[]
                {
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                    default(Guid?).AsRefOptional<IReferenceable>(),
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                },
                arrayRelatedOptionalRef = new[]
                {
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                    default(Guid?).AsRefOptional<RelatedModel>(),
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                },
                arrayString = new[] { "Barf", null, string.Empty, "bar", },
            };

            var resource = new ComplexStorageModel
            {
                resourceRef = resourceRef,
                guid = Guid.NewGuid(),
                objectInt = 3,
                objectString = "food",
                embeddedModel = embedded1,
                refObjObj = Guid.NewGuid().AsRef<IReferenceable>(),
                relatedOptionalObjObjRef = Guid.NewGuid().AsRefOptional<IReferenceable>(),
                relatedOptionalRef = Guid.NewGuid().AsRefOptional<RelatedModel>(),
                relatedRef = Guid.NewGuid().AsRef<RelatedModel>(),
                stringProperty = "barf",
                arrayGuid = new [] { Guid.NewGuid(), Guid.NewGuid() },
                arrayEnum = new []
                {
                    ComplexStorageModel.ExampleEnum.neutral,
                    ComplexStorageModel.ExampleEnum.ascending,
                    ComplexStorageModel.ExampleEnum.desending,
                },
                arrayObjectInt = new object[] { 1, 2 },
                arrayObjectString = new object[] { string.Empty, "foo", "bar", null },
                arrayRef = new[] { Guid.NewGuid().AsRef<RelatedModel>(), Guid.NewGuid().AsRef<RelatedModel>() },
                arrayRefObj = new[] { Guid.NewGuid().AsRef<RelatedModelObj>(), Guid.NewGuid().AsRef<RelatedModelObj>() },
                arrayRefObjObj = new[] { Guid.NewGuid().AsRef<IReferenceable>(), Guid.NewGuid().AsRef<IReferenceable>() },
                arrayRelatedOptionalObjObjRef = new[]
                {
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                    default(Guid?).AsRefOptional<IReferenceable>(),
                    Guid.NewGuid().AsRefOptional<IReferenceable>(),
                },
                arrayRelatedOptionalRef = new[]
                {
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                    default(Guid?).AsRefOptional<RelatedModel>(),
                    Guid.NewGuid().AsRefOptional<RelatedModel>(),
                },
                arrayString = new[] { "Bar", null, string.Empty, "Food", },
                arrayEmbeddedModel = new [] { embedded1, embedded2  },

                dictGuid = new Dictionary<Guid, Guid[]>
                {
                    { Guid.NewGuid(), new [] { Guid.NewGuid(), Guid.NewGuid() } },
                    { Guid.NewGuid(), new [] { Guid.NewGuid(), Guid.NewGuid() } },
                },
                dictObject = new Dictionary<object, object[]>
                {
                    { 1, new object [] { 33, "asdf", 44 } },
                    { "3", new object [] { Guid.NewGuid(), Guid.NewGuid() } },
                },
                //dictEmbeddedModel = new Dictionary<ComplexStorageModel.EmbeddedModel, ComplexStorageModel.EmbeddedModel[]>
                //{
                //    { embedded1, new [] { embedded2, embedded2, } },
                //    { embedded2, new [] { embedded2, embedded1, } },
                //},
            };
            Assert.IsTrue(await resource.StorageCreateAsync(
                (resourceIdCreated) => true,
                () =>
                {
                    return false;
                }));

            var resourceLoaded = await resourceRef.StorageGetAsync(
                rl => rl,
                () =>
                {
                    Assert.Fail("Failed to load resource.");
                    throw new Exception();
                });

            Assert.AreEqual(resource.id, resourceLoaded.id);
            Assert.AreEqual(resource.guid, resourceLoaded.guid);
            Assert.AreEqual(resource.objectInt, resourceLoaded.objectInt);
            Assert.AreEqual(resource.objectString, resourceLoaded.objectString);
            Assert.AreEqual(resource.stringProperty, resourceLoaded.stringProperty);

            #region Embedded object test

            Assert.AreEqual(resource.embeddedModel.guid, resourceLoaded.embeddedModel.guid);
            Assert.AreEqual(resource.embeddedModel.objectInt, resourceLoaded.embeddedModel.objectInt);
            Assert.AreEqual(resource.embeddedModel.objectString, resourceLoaded.embeddedModel.objectString);
            Assert.AreEqual(resource.embeddedModel.stringProperty, resourceLoaded.embeddedModel.stringProperty);

            Assert.AreEqual(resource.embeddedModel.relatedOptionalRef.id, resourceLoaded.embeddedModel.relatedOptionalRef.id);
            Assert.AreEqual(resource.embeddedModel.relatedRef.id, resourceLoaded.embeddedModel.relatedRef.id);

            Assert.AreEqual(resource.embeddedModel.arrayGuid[0], resourceLoaded.embeddedModel.arrayGuid[0]);
            Assert.AreEqual(resource.embeddedModel.arrayGuid[1], resourceLoaded.embeddedModel.arrayGuid[1]);
            Assert.AreEqual(resource.embeddedModel.arrayObjectInt[0], resourceLoaded.embeddedModel.arrayObjectInt[0]);
            Assert.AreEqual(resource.embeddedModel.arrayObjectInt[1], resourceLoaded.embeddedModel.arrayObjectInt[1]);
            Assert.AreEqual(resource.embeddedModel.arrayObjectString[0], resourceLoaded.embeddedModel.arrayObjectString[0]);
            Assert.AreEqual(resource.embeddedModel.arrayObjectString[1], resourceLoaded.embeddedModel.arrayObjectString[1]);
            Assert.AreEqual(resource.embeddedModel.arrayString[0], resourceLoaded.embeddedModel.arrayString[0]);
            Assert.AreEqual(resource.embeddedModel.arrayString[1], resourceLoaded.embeddedModel.arrayString[1]);
            Assert.AreEqual(resource.embeddedModel.arrayString[2], resourceLoaded.embeddedModel.arrayString[2]);
            Assert.AreEqual(resource.embeddedModel.arrayString[3], resourceLoaded.embeddedModel.arrayString[3]);
            Assert.AreEqual(resource.embeddedModel.arrayRef[0].id, resourceLoaded.embeddedModel.arrayRef[0].id);
            Assert.AreEqual(resource.embeddedModel.arrayRef[1].id, resourceLoaded.embeddedModel.arrayRef[1].id);
            Assert.AreEqual(resource.embeddedModel.arrayRefObjObj[0].id, resourceLoaded.embeddedModel.arrayRefObjObj[0].id);
            Assert.AreEqual(resource.embeddedModel.arrayRefObjObj[1].id, resourceLoaded.embeddedModel.arrayRefObjObj[1].id);
            Assert.AreEqual(resource.embeddedModel.arrayRelatedOptionalObjObjRef[0].id, resourceLoaded.embeddedModel.arrayRelatedOptionalObjObjRef[0].id);
            Assert.AreEqual(resource.embeddedModel.arrayRelatedOptionalObjObjRef[1].id, resourceLoaded.embeddedModel.arrayRelatedOptionalObjObjRef[1].id);
            Assert.AreEqual(resource.embeddedModel.arrayRelatedOptionalRef[0].id, resourceLoaded.embeddedModel.arrayRelatedOptionalRef[0].id);
            Assert.AreEqual(resource.embeddedModel.arrayRelatedOptionalRef[1].id, resourceLoaded.embeddedModel.arrayRelatedOptionalRef[1].id);

            #endregion

            Assert.AreEqual(resource.refObjObj.id, resourceLoaded.refObjObj.id);
            Assert.AreEqual(resource.relatedOptionalObjObjRef.id, resourceLoaded.relatedOptionalObjObjRef.id);
            Assert.AreEqual(resource.relatedOptionalRef.id, resourceLoaded.relatedOptionalRef.id);
            Assert.AreEqual(resource.relatedRef.id, resourceLoaded.relatedRef.id);
            Assert.AreEqual(resource.resourceRef.id, resourceLoaded.resourceRef.id);

            Assert.AreEqual(resource.arrayGuid.Length, resourceLoaded.arrayGuid.Length);
            Assert.AreEqual(resource.arrayGuid[0], resourceLoaded.arrayGuid[0]);
            Assert.AreEqual(resource.arrayGuid[1], resourceLoaded.arrayGuid[1]);

            Assert.AreEqual(resource.arrayEnum[0], resourceLoaded.arrayEnum[0]);
            Assert.AreEqual(resource.arrayEnum[1], resourceLoaded.arrayEnum[1]);
            Assert.AreEqual(resource.arrayEnum[2], resourceLoaded.arrayEnum[2]);

            Assert.AreEqual(resource.arrayObjectInt.Length, resourceLoaded.arrayObjectInt.Length);
            Assert.AreEqual(resource.arrayObjectInt[0], resourceLoaded.arrayObjectInt[0]);
            Assert.AreEqual(resource.arrayObjectInt[1], resourceLoaded.arrayObjectInt[1]);

            Assert.AreEqual(resource.arrayObjectString.Length, resourceLoaded.arrayObjectString.Length);
            Assert.AreEqual(resource.arrayObjectString[0], resourceLoaded.arrayObjectString[0]);
            Assert.AreEqual(resource.arrayObjectString[1], resourceLoaded.arrayObjectString[1]);
            Assert.AreEqual(resource.arrayObjectString[2], resourceLoaded.arrayObjectString[2]);
            Assert.AreEqual(resource.arrayObjectString[3], resourceLoaded.arrayObjectString[3]);

            Assert.AreEqual(resource.arrayRef.Length, resourceLoaded.arrayRef.Length);
            Assert.AreEqual(resource.arrayRef[0].id, resourceLoaded.arrayRef[0].id);
            Assert.AreEqual(resource.arrayRef[1].id, resourceLoaded.arrayRef[1].id);

            Assert.AreEqual(resource.arrayRefObj.Length, resourceLoaded.arrayRefObj.Length);
            Assert.AreEqual(resource.arrayRefObj[0].id, resourceLoaded.arrayRefObj[0].id);
            Assert.AreEqual(resource.arrayRefObj[1].id, resourceLoaded.arrayRefObj[1].id);

            Assert.AreEqual(resource.arrayRefObjObj.Length, resourceLoaded.arrayRefObjObj.Length);
            Assert.AreEqual(resource.arrayRefObjObj[0].id, resourceLoaded.arrayRefObjObj[0].id);
            Assert.AreEqual(resource.arrayRefObjObj[1].id, resourceLoaded.arrayRefObjObj[1].id);

            Assert.AreEqual(resource.arrayRelatedOptionalObjObjRef.Length, resourceLoaded.arrayRelatedOptionalObjObjRef.Length);
            Assert.AreEqual(resource.arrayRelatedOptionalObjObjRef[0].id, resourceLoaded.arrayRelatedOptionalObjObjRef[0].id);
            Assert.AreEqual(resource.arrayRelatedOptionalObjObjRef[1].id, resourceLoaded.arrayRelatedOptionalObjObjRef[1].id);
            Assert.AreEqual(resource.arrayRelatedOptionalObjObjRef[2].id, resourceLoaded.arrayRelatedOptionalObjObjRef[2].id);

            Assert.AreEqual(resource.arrayRelatedOptionalRef.Length, resourceLoaded.arrayRelatedOptionalRef.Length);
            Assert.AreEqual(resource.arrayRelatedOptionalRef[0].id, resourceLoaded.arrayRelatedOptionalRef[0].id);
            Assert.AreEqual(resource.arrayRelatedOptionalRef[1].id, resourceLoaded.arrayRelatedOptionalRef[1].id);
            Assert.AreEqual(resource.arrayRelatedOptionalRef[2].id, resourceLoaded.arrayRelatedOptionalRef[2].id);

            Assert.AreEqual(resource.arrayString.Length, resourceLoaded.arrayString.Length);
            Assert.AreEqual(resource.arrayString[0], resourceLoaded.arrayString[0]);
            Assert.AreEqual(resource.arrayString[1], resourceLoaded.arrayString[1]);
            Assert.AreEqual(resource.arrayString[2], resourceLoaded.arrayString[2]);
            Assert.AreEqual(resource.arrayString[3], resourceLoaded.arrayString[3]);

            // TODO: Dictionaries

            Assert.AreEqual(resource.dictGuid[resource.dictGuid.Keys.First()][0], resourceLoaded.dictGuid[resource.dictGuid.Keys.First()][0]);
            Assert.AreEqual(resource.dictGuid[resource.dictGuid.Keys.Skip(1).First()][1], resourceLoaded.dictGuid[resource.dictGuid.Keys.Skip(1).First()][1]);

            Assert.AreEqual(resource.dictObject[resource.dictObject.Keys.First()][0], resourceLoaded.dictObject[resource.dictObject.Keys.First()][0]);
            Assert.AreEqual(resource.dictObject[resource.dictObject.Keys.Skip(1).First()][1], resourceLoaded.dictObject[resource.dictObject.Keys.Skip(1).First()][1]);

            //Assert.AreEqual(resource.dictEmbeddedModel[resource.dictEmbeddedModel.Keys.First()][0].arrayRef[0].id, resourceLoaded.dictEmbeddedModel[resource.dictEmbeddedModel.Keys.First()][0].arrayRef[0].id);
            //Assert.AreEqual(resource.dictEmbeddedModel[resource.dictEmbeddedModel.Keys.Skip(1).First()][1].stringProperty, resourceLoaded.dictEmbeddedModel[resource.dictEmbeddedModel.Keys.Skip(1).First()][1].stringProperty);

        }
    }
}