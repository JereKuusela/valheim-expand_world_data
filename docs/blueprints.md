# Blueprints

Dungeons, locations and vegetation support using blueprints to spawn multiple objects. Both PlanBuild .blueprint and BuildShare .vbuild files are supported (however PlanBuild files are preferred).

The file format is slightly modified from the usual:

- Two new fields are added to both .blueprint and .vbuild files.
  - zdoData initializes the object with a specific data. Infinity Hammer automatically saves this when creating .blueprint files.
  - chance is a number between 0 and 1. These must be added manually to the file.
  - .blueprint format: name;unused;posX;posY;posZ;rotX;rotT;rotZ;rotW;info;scaleX;scaleY;scaleZ;zdoData;chance
  - .vbuild format: name;rotX;rotT;rotZ;rotW;posX;posY;posZ;zdoData;chance
- Blueprints can contain other blueprints as objects. These must added manually to the file.
- Center piece (bottom center of the blueprint) can be set to a certain object. This object is never spawned to the world.
  - Infinity Hammer saves this information to .blueprint files.
  - If the center piece is not found, the blueprint is centered automatically and placed 0.05 meters towards the ground.
