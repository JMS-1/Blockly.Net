using BlocklyNet.Core.Model;
using BlocklyNet.Extensions;
using BlocklyNet.Extensions.Models;
using NUnit.Framework;

namespace BlocklyNetTests.CoreEx;

[TestFixture]
public class XmlTests : TestEnvironment
{
    private Context Context = null!;

    protected override void OnStartup()
    {
        Context = new Context(Site.Object);

        base.OnStartup();
    }

    private const string Sample1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <CATALOG>
            <CD mode=""oldie"">
                <TITLE>Empire Burlesque</TITLE>
                <ARTIST>Bob Dylan</ARTIST>
                <COUNTRY>USA</COUNTRY>
                <COMPANY>Columbia</COMPANY>
                <PRICE>10.90</PRICE>
                <YEAR>1985</YEAR>
            </CD>
            <CD>
                <TITLE>Hide your heart</TITLE>
                <ARTIST>Bonnie Tyler</ARTIST>
                <COUNTRY>UK</COUNTRY>
                <COMPANY>CBS Records</COMPANY>
                <PRICE>9.90</PRICE>
                <YEAR>1988</YEAR>
            </CD>
            <CD>
                <TITLE>Greatest Hits</TITLE>
                <ARTIST>Dolly Parton</ARTIST>
                <COUNTRY>USA</COUNTRY>
                <COMPANY>RCA</COMPANY>
                <PRICE>9.90</PRICE>
                <YEAR>1982</YEAR>
            </CD>
        </CATALOG>";

    [Test]
    public async Task Can_Parse_Xml_Async()
    {
        /* Build block tree. */
        var block = new CreateXmlDocument
        {
            Values = { new() { Name = "CONTENT", Block = CreateStringBlock(Sample1) } }
        };

        /* Parse XML. */
        var file = await block.EnterBlockAsync(new Context(Site.Object));

        /* Validate result. */
        Assert.That(file, Is.TypeOf<XmlFile>());
    }

    [Test]
    public async Task Can_Query_Xml_Document_Async()
    {
        /* Build block tree. */
        var block = new QueryXmlDocument
        {
            Values = {
                new() { Name = "SOURCE", Block = new CreateXmlDocument{ Values = { new() { Name = "CONTENT", Block = CreateStringBlock(Sample1) } } } },
                new() { Name = "XPATH", Block = CreateStringBlock("CATALOG/CD[@mode=\"oldie\"]") }
            }
        };

        /* Parse XML. */
        var value = await block.EnterBlockAsync(new Context(Site.Object));
        var nodes = ((IEnumerable<XmlNode>)value!).ToList();

        /* Validate result. */
        Assert.That(nodes, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(nodes[0].Name, Is.EqualTo("CD"));
            Assert.That(nodes[0].LocalName, Is.EqualTo("CD"));
            Assert.That(nodes[0].Namespace, Is.EqualTo(""));
            Assert.That(nodes[0].Content, Is.EqualTo("Empire BurlesqueBob DylanUSAColumbia10.901985"));
            Assert.That(nodes[0].Attributes, Has.Count.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(nodes[0].Attributes[0].Name, Is.EqualTo("mode"));
            Assert.That(nodes[0].Attributes[0].LocalName, Is.EqualTo("mode"));
            Assert.That(nodes[0].Attributes[0].Namespace, Is.EqualTo(""));
            Assert.That(nodes[0].Attributes[0].Value, Is.EqualTo("oldie"));
        });
    }

    [Test]
    public async Task Can_Query_Xml_Node_Async()
    {
        /* Build block tree. */
        var block = new QueryXmlDocument
        {
            Values = {
                new() { Name = "SOURCE", Block = new CreateXmlDocument{ Values = { new() { Name = "CONTENT", Block = CreateStringBlock(Sample1) } } } },
                new() { Name = "XPATH", Block = CreateStringBlock("CATALOG/CD[@mode=\"oldie\"]") }
            }
        };

        /* Parse XML. */
        var value = await block.EnterBlockAsync(new Context(Site.Object));
        var nodes = ((IEnumerable<XmlNode>)value!).ToList();

        /* Build block tree. */
        block = new QueryXmlDocument
        {
            Values = {
                new() { Name = "SOURCE", Block = new AnyValueBlock(nodes[0])},
                new() { Name = "XPATH", Block = CreateStringBlock("ARTIST") }
            }
        };

        /* Execute. */
        value = await block.EnterBlockAsync(new Context(Site.Object));
        nodes = ((IEnumerable<XmlNode>)value!).ToList();

        /* Validate result. */
        Assert.That(nodes, Has.Count.EqualTo(1));

        Assert.That(nodes[0].Name, Is.EqualTo("ARTIST"));
        Assert.That(nodes[0].LocalName, Is.EqualTo("ARTIST"));
        Assert.That(nodes[0].Namespace, Is.EqualTo(""));
        Assert.That(nodes[0].Content, Is.EqualTo("Bob Dylan"));
        Assert.That(nodes[0].Attributes, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task Can_Add_To_Parent_XmlFile_Async()
    {
        XmlFile xmlFile = new();
        XmlAttribute testAttribute = new() { Name = "attrName", Value = "attrValue" };

        var testNode = new XmlNode { Name = "test", Content = "test2", LocalName = "test3", Attributes = [testAttribute] };
        /* Build block tree. */
        var block = new AddToXmlParent
        {
            Values = {
                new() { Name = "PARENT", Block = new AnyValueBlock(xmlFile)},
                new() { Name = "CONTENT", Block = new AnyValueBlock(testNode)
            }
        }
        };

        await block.EnterBlockAsync(Context);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(xmlFile.Document.FirstChild!.Attributes![0].Name, Is.EqualTo("attrName"));
            Assert.That(xmlFile.Document.FirstChild!.Attributes![0].Value, Is.EqualTo("attrValue"));
            Assert.That(xmlFile.Document.FirstChild!.InnerText, Is.EqualTo("test2"));
        }

        XmlNode node = testNode;
        var blockNode = new AddToXmlParent
        {
            Values = {
                new() { Name = "PARENT", Block = new AnyValueBlock(node)},
                new() { Name = "CONTENT", Block = new AnyValueBlock(new XmlNode{ Name = "foo", Content = "bar", LocalName = "foobar", Attributes = [testAttribute] })
            }
        }
        };

        await blockNode.EnterBlockAsync(Context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(xmlFile.Document.FirstChild!.LastChild!.Attributes![0].Name, Is.EqualTo("attrName"));
            Assert.That(xmlFile.Document.FirstChild!.LastChild!.Attributes![0].Value, Is.EqualTo("attrValue"));
            Assert.That(xmlFile.Document.FirstChild!.LastChild!.InnerText, Is.EqualTo("bar"));
        }
    }

    [Test]
    public async Task Can_Add_To_Parent_String_Async()
    {
        XmlFile xmlFile = new();

        var test = "<test id=\"123\">hello world</test>";
        /* Build block tree. */
        var block = new AddToXmlParent
        {
            Values = {
                new() { Name = "PARENT", Block = new AnyValueBlock(xmlFile)},
                new() { Name = "CONTENT", Block = CreateStringBlock(test)
            }
        }
        };

        await block.EnterBlockAsync(Context);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(xmlFile.Document.FirstChild!.Attributes![0].Name, Is.EqualTo("id"));
            Assert.That(xmlFile.Document.FirstChild!.Attributes![0].Value, Is.EqualTo("123"));
            Assert.That(xmlFile.Document.FirstChild!.InnerText, Is.EqualTo("hello world"));
        }

        var testNode = xmlFile.Query("//test")[0];
        var blockNode = new AddToXmlParent
        {
            Values = {
                new() { Name = "PARENT", Block = new AnyValueBlock(testNode)},
                new() { Name = "CONTENT", Block = CreateStringBlock("<foo id2=\"456\">bar</foo>")
            }
        }
        };

        await blockNode.EnterBlockAsync(Context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(testNode.Node.LastChild!.Attributes![0].Value, Is.EqualTo("456"));
            Assert.That(testNode.Node.LastChild!.Attributes![0].Name, Is.EqualTo("id2"));
            Assert.That(testNode.Node.LastChild!.InnerText, Is.EqualTo("bar"));
            Assert.That(xmlFile.Document.OuterXml, Is.EqualTo("<test id=\"123\">hello world<foo id2=\"456\">bar</foo></test>"));
        }
    }
}