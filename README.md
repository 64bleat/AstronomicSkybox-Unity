# Astronomic Skybox
## [Documentation Wiki](https://github.com/64bleat/AstronomicSkybox-Unity/wiki)
## [Install through Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

## Description 
Astronomic Skybox is a Unity Package for simulating realistic skyboxes. Perfect for recreating real-world places, time lapse effects, impressive day/night cycles, or just stargazing!

![Example 1](.github/ex1.jpg)

![Example 2](.github/ex2.jpg)

## Use
Simply drop the `Skybox` prefab and a `TimeManager` singleton into your scene and you will have a working skybox! Read the **wiki** to learn what options are available.

## Features
* Accurate positioning of the sun, moon, and stars.
* DirectionalLights to simulate sunlight, moonlight, and ambient light
* Supports three celestial coordinate systems
    *  The **Horizontal coordinate system** is the view from a given point on earth.
    *  The **Equatorial coordinate system** is centered around Earth and its equator. This is what the stars are mapped to.
    *  The **Ecliptic coordinate system** is centered around Earth in this instance and the orbital plane of the planets.
* `StarPositioner.cs` orients the skybox to be accurate to specific dates, times and places on Earth.
* `StarGenerator.cs` generates stars and constellations for the skybox.
* `StarMath.cs` contains a bunch of methods for converting coordinate systems and calculating the position of the sun and moon.

[stellarData.txt is sourced from astronexus/HYG-Database](https://github.com/astronexus/HYG-Database/blob/master/hygdata_v3.csv)
