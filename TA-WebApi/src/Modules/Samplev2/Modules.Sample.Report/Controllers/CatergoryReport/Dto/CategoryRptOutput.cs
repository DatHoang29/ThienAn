namespace Modules.Sample.Report.Controllers.CatergoryReport.Dto;

public class CategoryRptOutput
{
    public Parameter Parameter { get; set; }
    public Column Column { get; set; }
    public List<Data> Data { get; set; }
}

//Thong so
public class Parameter
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Footer { get; set; }
}

//Ngon ngu
public class Column
{
    public string Name { get; set; } = "Name";
    public string Description { get; set; } = "Description";
    public string Value { get; set; } = "Value";

}

//Du liệu

public class Data
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Random { get; set; }
}