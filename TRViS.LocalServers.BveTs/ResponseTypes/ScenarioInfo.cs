using System;

namespace TRViS.LocalServers.BveTs.ResponseTypes;

/// <summary>
/// 現在開かれているシナリオの情報
/// </summary>
public class ScenarioInfo(
	string routeName,
	string scenarioName,
	string carName
) : IEquatable<ScenarioInfo>
{
	/// <summary>
	/// 路線名
	/// </summary>
	public string RouteName { get; set; } = routeName;

	/// <summary>
	/// シナリオ名 (列車番号や種別など)
	/// </summary>
	public string ScenarioName { get; set; } = scenarioName;

	/// <summary>
	/// 車両名
	/// </summary>
	public string CarName { get; set; } = carName;

	/// <summary>
	/// 空のインスタンスを初期化する
	/// </summary>
	public ScenarioInfo() : this(
		routeName: string.Empty,
		scenarioName: string.Empty,
		carName: string.Empty
	)
	{ }

	public override string ToString()
		=> $"{nameof(ScenarioInfo)}{{Route:{RouteName}, Scenario:{ScenarioName}, Car:{CarName}}}";

	public bool Equals(ScenarioInfo? other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return (
			RouteName == other.RouteName
			&&
			ScenarioName == other.ScenarioName
			&&
			CarName == other.CarName
		);
	}

	public override bool Equals(object? obj)
		=> Equals(obj as ScenarioInfo);

	public override int GetHashCode()
		=> (
			RouteName.GetHashCode()
			^
			ScenarioName.GetHashCode()
			^
			CarName.GetHashCode()
		);
}
