using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Orbital;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>
/// Тесты масштаба системы: пропорции размеров и расстояний должны быть «реалистичными» —
/// звезда заметно крупнее планет, орбиты многократно превышают радиусы тел.
/// </summary>
public sealed class SystemScaleTests
{
    /// <summary>Компактные объекты, для которых «звезда крупнее планеты» не выполняется по физике.</summary>
    private static readonly SpectralClass[] CompactObjects = { SpectralClass.NeutronStar, SpectralClass.BlackHole };

    [Fact]
    public void StarIsLargerThanEveryPlanet()
    {
        foreach (SpectralClass star in Enum.GetValues<SpectralClass>())
        {
            if (CompactObjects.Contains(star))
            {
                continue; // У нейтронной звезды радиус физически меньше газового гиганта.
            }

            double starRadius = SystemScale.StarRadius(star);
            foreach (PlanetType type in Enum.GetValues<PlanetType>())
            {
                Assert.True(starRadius > SystemScale.PlanetRadius(type),
                    $"Звезда ({star}, r={starRadius}) должна быть больше планеты ({type}).");
            }
        }
    }

    [Fact]
    public void StarIsAtLeastTwiceLargestPlanet()
    {
        // Даже для самой маленькой «обычной» звезды и крупнейшей планеты — заметная разница.
        double smallestStar = Enum.GetValues<SpectralClass>()
            .Where(s => !CompactObjects.Contains(s))
            .Min(SystemScale.StarRadius);
        double largestPlanet = Enum.GetValues<PlanetType>().Max(SystemScale.PlanetRadius);
        Assert.True(smallestStar >= largestPlanet * 2.0,
            $"Соотношение звезда/планета слишком мало ({smallestStar} vs {largestPlanet}).");
    }

    [Fact]
    public void InnerOrbit_MuchLargerThanStarRadius()
    {
        foreach (SpectralClass star in Enum.GetValues<SpectralClass>())
        {
            Assert.True(SystemScale.InnerOrbit > SystemScale.StarRadius(star) * 1.5,
                $"Внутренняя орбита должна быть заметно дальше поверхности звезды ({star}).");
        }
    }

    [Fact]
    public void PlanetOrbits_GrowGeometrically()
    {
        for (int i = 1; i < 8; i++)
        {
            double prev = SystemScale.OrbitSemiMajorAxis(i - 1);
            double current = SystemScale.OrbitSemiMajorAxis(i);
            Assert.True(current > prev * 1.3, "Орбиты должны заметно расходиться (геометрический шаг).");
        }
    }

    [Fact]
    public void Orbit_IsManyPlanetRadii()
    {
        // Расстояние до планеты многократно превышает её радиус (как в реальной системе).
        StarSystemData system = StarSystemGenerator.GenerateSystem(GalaxyGenerator.GenerateGalaxy(7, 0), 0);
        var model = new StarSystemModel(system);

        for (int i = 0; i < model.BodyCount; i++)
        {
            double orbitRadius = model.Body(i).Orbit.SemiMajorAxis;
            Assert.True(orbitRadius > model.Body(i).Radius * 20.0,
                $"Орбита планеты {i} ({orbitRadius:0}) должна быть много больше её радиуса ({model.Body(i).Radius:0}).");
        }
    }

    [Fact]
    public void Model_UsesScaleForStarAndPlanets()
    {
        StarSystemData system = StarSystemGenerator.GenerateSystem(GalaxyGenerator.GenerateGalaxy(9, 0), 0);
        var model = new StarSystemModel(system);

        Assert.Equal(SystemScale.StarRadius(system.Star), model.StarRadius);
        Assert.Equal(SystemScale.GravitationalParameter(system.Star), model.Mu);
        for (int i = 0; i < model.BodyCount; i++)
        {
            Assert.Equal(SystemScale.PlanetRadius(model.Body(i).Planet.Type), model.Body(i).Radius);
        }
    }

    [Fact]
    public void DockRadius_IsSurfacePlusMargin()
    {
        StarSystemData system = StarSystemGenerator.GenerateSystem(GalaxyGenerator.GenerateGalaxy(11, 0), 0);
        var model = new StarSystemModel(system);
        Assert.Equal(model.Body(0).Radius + SystemScale.DockingMargin, model.DockRadius(0));
    }
}
