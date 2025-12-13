using explainpowershell.models;

namespace explainpowershell.frontend.tests;

public class TreeTests
{
    [Test]
    public void GenerateTree_TreatsNullAndEmptyParentIdAsRoot()
    {
        var explanations = new List<Explanation>
        {
            new() { Id = "1", ParentId = "", CommandName = "root-empty" },
            new() { Id = "2", ParentId = null, CommandName = "root-null" },
            new() { Id = "1.1", ParentId = "1", CommandName = "child" },
        };

        var tree = explanations.GenerateTree(e => e.Id, e => e.ParentId);

        Assert.That(tree, Has.Count.EqualTo(2));

        var rootEmpty = tree.Single(t => t.Value is not null && t.Value.Id == "1");
        Assert.That(rootEmpty.Value, Is.Not.Null);
        Assert.That(rootEmpty.Children, Is.Not.Null);
        var rootEmptyChildren = rootEmpty.Children!;
        Assert.That(rootEmptyChildren, Has.Count.EqualTo(1));
        var onlyChild = rootEmptyChildren.Single();
        Assert.That(onlyChild.Value, Is.Not.Null);
        Assert.That(onlyChild.Value!.Id, Is.EqualTo("1.1"));

        var rootNull = tree.Single(t => t.Value is not null && t.Value.Id == "2");
        Assert.That(rootNull.Value, Is.Not.Null);
        Assert.That(rootNull.Children, Is.Null);
    }
}
