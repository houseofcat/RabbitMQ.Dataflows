### Introduction to Xformers!

#### Intro  

I have written a guide for setting up AUTOMATIC1111's stable diffusion locally over 
[here](https://houseofcat.io/guides/ml/stablediffusion/setuplocally). This is a quick tutorial 
on enabling Xformers how it can speed up image generation and lower VRAM usage.

#### AUTOMATIC1111's WEB UI with Xformers Enabled

To enable the Xformers usage on modern nVidia GPUs, all you have to do now is add a command line
argument that will get called by your user BAT file.

```text
webui-user.bat
```

```text
@echo off

...

set COMMANDLINE_ARGS=--xformers

...
```

Everything after the `=` is treated a string so to add more command line parameters just add a space
between command line arguments.  

Example.) Also hide progress bars.  

```text
set COMMANDLINE_ARGS=--xformers --no-progressbar-hiding
```

Here is a full list of `COMMANDLINE_ARGS` supported [here](https://github.com/AUTOMATIC1111/stable-diffusion-webui/wiki/Command-Line-Arguments-and-Settings).

#### What Is Xformers?

Flexible Transformers, defined by interoperable and optimized building blocks. You can read everything
at [Facebook](https://facebookresearch.github.io/xformers/factory/index.html).

The main advantage here is for people with slightly weaker GPUs to get more results quicker and in my
humble opinion seems more stable.  

#### There is a Cost

Due to the optimizations made, you do lose a bit of determinism which means replay/repeatability. This
means that if you adjust the settings to re-render you can get a difference in the outputs even with
a seed. It may not be radically different but I find this extremely useful if I want to lock something in but
constantly re-render variations quicklys.

### Orange Cat 01 - Without Xformers

This is a baseline image.  

![OrangeCat01](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/xformers/OrangeCat01.png)

#### Text Prompt
```text
An ((orange cat)) sitting on the roof of a (skyscraper) in a (cyberpunk city)
at night while its ((raining)), photo realistic, 8k
```

#### Negative Prompt
```text
bad-artist
```

#### Settings
```text
Steps: 100  
Sampler: Euler a
CFG scale: 9
Seed: 3562015388
Size: 768x768
Model hash: 2c02b20a
```

#### Usage Details
```text
GPU: RTX 3090
Render Time: 16 seconds
VRAM @ Idle: 5440 MB
VRAM @ Max: 9860 MB
Actual MAX VRAM Usage: 4420 MB
```


### Orange Cat 02 - Without Xformers

The expectation is that there is no change on re-run.

![OrangeCat02](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/xformers/OrangeCat02.png)

#### Text Prompt
```text
An ((orange cat)) sitting on the roof of a (skyscraper) in a (cyberpunk city)
at night while its ((raining)), photo realistic, 8k
```

#### Negative Prompt
```text
bad-artist
```

#### Settings
```text
Steps: 100  
Sampler: Euler a
CFG scale: 9
Seed: 3562015388
Size: 768x768
Model hash: 2c02b20a
```

#### Usage Details
```text
GPU: RTX 3090
Render Time: 16 seconds
VRAM @ Idle: 5420 MB
VRAM @ Max: 9860 MB
Actual MAX VRAM Usage: 4440 MB
```

### Orange Cat 03 - With Xformers

There maybe expected small changes but used significantly lower VRAM amount and should have
rendered faster.

![OrangeCat03](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/xformers/OrangeCat03.png)

#### Text Prompt
```text
An ((orange cat)) sitting on the roof of a (skyscraper) in a (cyberpunk city)
at night while its ((raining)), photo realistic, 8k
```

#### Negative Prompt
```text
bad-artist
```

#### Settings
```text
Steps: 100  
Sampler: Euler a
CFG scale: 9
Seed: 3562015388
Size: 768x768
Model hash: 2c02b20a
```

#### Usage Details
```text
GPU: RTX 3090
Render Time: 14 seconds
VRAM @ Idle: 5422 MB
VRAM @ Max: 6076 MB
Actual MAX VRAM Usage: 654 MB
```

### Orange Cat 04 - With Xformers

![OrangeCat04](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/xformers/OrangeCat04.png)

#### Text Prompt
```text
An ((orange cat)) sitting on the roof of a (skyscraper) in a (cyberpunk city) at night while its ((raining)), photo realistic, 8k
```

#### Settings
```text
Steps: 100  
Sampler: Euler a
CFG scale: 9
Seed: 3562015388
Size: 768x768
Model hash: 2c02b20a
```

#### Usage Details
```text
GPU: RTX 3090
Render Time: 13 seconds
VRAM @ Idle: 5422 MB
VRAM @ Max: 6077 MB
Actual MAX VRAM Usage: 655 MB
```

### Usage Summary

There weren't variations this time around, but it doesn't mean they couldn't have happened with slightly different settings.

Shaved 3 seconds off of render time but the real highlight is that with Xformers, it used ~650 MB of VRAM vs. ~4400 MB of VRAM
to output nearly identical images. This is a huge saving in VRAM!