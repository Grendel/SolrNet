using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using SolrNet.Commands.Parameters;
using SolrNet.Utils;

namespace SolrNet.Tests {
	[TestFixture]
	public class SolrQueryExecuterTests {
		public class TestDocument : ISolrDocument {
			[SolrUniqueKey]
			public int Id { get; set; }
		}

		[Test]
		public void Execute() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var mockR = mocks.DynamicMock<ISolrQueryResults<TestDocument>>();
			With.Mocks(mocks).Expecting(delegate {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				Expect.Call(conn.Get("/select", q)).Repeat.Once().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Repeat.Once().Return(mockR);
			}).Verify(delegate {
				var queryExecuter = new SolrQueryExecuter<TestDocument>(conn, queryString) {ResultParser = parser};
				var r = queryExecuter.Execute();
			});
		}

		[Test]
		public void Sort() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var mockR = mocks.DynamicMock<ISolrQueryResults<TestDocument>>();
			With.Mocks(mocks).Expecting(delegate {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				q["rows"] = int.MaxValue.ToString();
				q["sort"] = "id asc";
				Expect.Call(conn.Get("/select", q)).Repeat.Once().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Repeat.Once().Return(mockR);
			}).Verify(delegate {
				var queryExecuter = new SolrQueryExecuter<TestDocument>(conn, queryString) {
					ResultParser = parser,
					Options = new QueryOptions {OrderBy = new[] {new SortOrder("id")}}
				};
				var r = queryExecuter.Execute();
			});
		}

		[Test]
		public void SortMultipleWithOrders() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var mockR = mocks.DynamicMock<ISolrQueryResults<TestDocument>>();
			With.Mocks(mocks).Expecting(delegate {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				q["rows"] = int.MaxValue.ToString();
				q["sort"] = "id asc,name desc";
				Expect.Call(conn.Get("/select", q)).Repeat.Once().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Repeat.Once().Return(mockR);
			}).Verify(delegate {
				var queryExecuter = new SolrQueryExecuter<TestDocument>(conn, queryString) {
					ResultParser = parser,
					Options = new QueryOptions {
						OrderBy = new[] {
							new SortOrder("id", Order.ASC),
							new SortOrder("name", Order.DESC)
						}
					}
				};
				var r = queryExecuter.Execute();
			});
		}

		[Test]
		public void RandomSort() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var random = mocks.CreateMock<IListRandomizer>();
			With.Mocks(mocks).Expecting(() => {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				q["rows"] = int.MaxValue.ToString();
				q["fl"] = "id";
				Expect.Call(conn.Get("/select", q)).IgnoreArguments().Return("");
				var doc123 = new TestDocument {Id = 123};	
				var doc456 = new TestDocument {Id = 456};
				var doc567 = new TestDocument {Id = 567};
				Expect.Call(parser.Parse(null)).IgnoreArguments().Return(new SolrQueryResults<TestDocument> {
					doc123,
					doc456,
					doc567,
				});
				Expect.Call(() => random.Randomize(new List<TestDocument>())).IgnoreArguments();
				var nq = new Dictionary<string, string>();
				nq["q"] = "(id:123 OR id:456 OR id:567)";
				Expect.Call(conn.Get("/select", nq)).IgnoreArguments().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Return(new SolrQueryResults<TestDocument> {
					doc123,
					doc456,
					doc567,
				});
			}).Verify(() => {
				var e = new SolrQueryExecuter<TestDocument>(conn, queryString) {
					ResultParser = parser,
					ListRandomizer = random,
					Options = new QueryOptions {
						OrderBy = SortOrder.Random,
						Rows = 2,
					}
				};
				var r = e.Execute();
			});
		}

		[Test]
		public void ResultFields() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var mockR = mocks.DynamicMock<ISolrQueryResults<TestDocument>>();
			With.Mocks(mocks).Expecting(delegate {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				q["rows"] = int.MaxValue.ToString();
				q["fl"] = "id,name";
				Expect.Call(conn.Get("/select", q)).Repeat.Once().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Repeat.Once().Return(mockR);
			}).Verify(delegate {
				var queryExecuter = new SolrQueryExecuter<TestDocument>(conn, queryString) {
					ResultParser = parser,
					Options = new QueryOptions {
						Fields = new[] {"id", "name"},
					}
				};
				var r = queryExecuter.Execute();
			});
		}

		[Test, Ignore("incomplete")]
		public void UndefinedFieldError() {
			const string queryString = "id:123456";
			var mocks = new MockRepository();
			var conn = mocks.CreateMock<ISolrConnection>();
			var parser = mocks.CreateMock<ISolrQueryResultParser<TestDocument>>();
			var mockR = mocks.DynamicMock<ISolrQueryResults<TestDocument>>();
			With.Mocks(mocks).Expecting(delegate {
				var q = new Dictionary<string, string>();
				q["q"] = queryString;
				Expect.Call(conn.Get("/select", q)).Repeat.Once().Return("");
				Expect.Call(parser.Parse(null)).IgnoreArguments().Repeat.Once().Return(mockR);
			}).Verify(delegate {
				var queryExecuter = new SolrQueryExecuter<TestDocument>(conn, queryString) {
					ResultParser = parser
				};
				var r = queryExecuter.Execute();
			});
		}
	}
}