namespace CmcTs.Core.Import;

// Tách riêng khỏi EstimateImportParser để dùng lại được ở màn Preview: sau khi Admin sửa tay
// Level/thứ tự 1 danh sách phẳng (vd gộp/tách dòng, đổi cấp cho đúng), gọi lại đúng thuật toán
// này để dựng lại cây + tính rollup, đảm bảo nhất quán với lúc parse lần đầu.
public static class TaskTreeBuilder
{
    // Cha của 1 dòng là dòng gần nhất phía trước có level nhỏ hơn (dựng bằng stack theo thứ tự đọc).
    public static List<ParsedTaskNode> Build(List<ParsedTaskNode> flat, List<string>? warnings = null)
    {
        var roots = new List<ParsedTaskNode>();
        var stack = new Stack<ParsedTaskNode>();

        foreach (var node in flat)
        {
            node.Children.Clear();

            while (stack.Count > 0 && stack.Peek().Level >= node.Level)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                if (node.Level != 1)
                {
                    warnings?.Add($"Dòng {node.SourceRow}: \"{node.Name}\" không tìm được mục cha phù hợp, đưa lên cấp cao nhất — cần kiểm tra lại.");
                }
                roots.Add(node);
            }
            else
            {
                stack.Peek().Children.Add(node);
            }

            stack.Push(node);
        }

        RollUp(roots);
        return roots;
    }

    private static void RollUp(List<ParsedTaskNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Count == 0)
            {
                continue;
            }

            RollUp(node.Children);
            node.MandayPlan = node.Children.Sum(c => c.MandayPlan);
            node.CostPlan = node.Children.Sum(c => c.CostPlan);
        }
    }
}
