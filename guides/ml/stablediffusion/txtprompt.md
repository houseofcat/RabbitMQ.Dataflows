### Introducting A Text Prompt Workflow!

#### Intro  

I have written a guide for setting up AUTOMATIC1111's stable diffusion locally over 
[here](https://houseofcat.io/guides/ml/stablediffusion/setuplocally). This is an introductory
guide to text prompting but more specifically with AUTOMATIC1111's prompts for it's Web-UI.   

#### AUTOMATIC1111 Text Prompt Syntax

So one of the useful features of using AUTOMATIC1111's web-ui for stable diffusion as 
it adds very powerful tools to really create more usefully accurate images.

```text
Attention, specify parts of text that the model should pay more attention to
 - a man in a ((tuxedo)) - will pay more attention to tuxedo
 - a man in a (tuxedo:1.21) - alternative syntax
 - select text and press ctrl+up or ctrl+down to automatically adjust attention to selected text (code contributed by anonymous user)
 ```
 
### Basic Prompting with Basic Settings for Stable Diffusion v2.x
Let's get a baseline image to work with and see where it takes us.  

#### Text Prompt
```text
A black fluffy cat wearing a scarf
```

#### Settings
```text
Steps: 100
Sampler: Euler a
CFG scale: 9
Seed: -1
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 1
![BlackCatScarf01](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf01.png)

#### Output Image Seed
```text
Seed: 559696028
```

### Next, Add a Negative Prompt
I am going to re-render this same cat using all the same settings, but I would like to add a negative prompt. I have a hunch
this will improve the facial features of the black cat in a scarf.  

I have learned the following can be used as an all encompassing tag to eliminate a lot of bad art. A negative prompt helps
tell the AI to heavily avoid this __*thing*__. So for example, if I was generating female Greek marble statues, and it kept
rendering male statues, I would add `male` to the negative prompt to help avoid this. Thinking negatively often comes
intuitively to a  engineesoftwarer so I encourage you to experiment with this. You might have a perfectly solid text prompt
completely ruined because you need to cherry pick out influences.  

#### Adding a Negative Prompt
```text
bad-artist
```

#### Settings
```text
Steps: 100
Sampler: Euler a
CFG scale: 9
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 2
![BlackCatScarf02](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf02.png)

### Adjusting the Text Prompt
Now we are going to adjust the text prompt to include a background scene.

#### Text Prompt
```text
A black fluffy cat wearing a white scarf walking through a city at night
```

#### Settings
```text
Steps: 100
Sampler: Euler a
CFG scale: 9
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 3
![BlackCatScarf03](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf03.png)

### Text Emphasis Added
Now we are going to add emphasis to the text prompt to help the AI focus on the scarf and cat. The cat is getting a little mutated.  

#### Text Prompt
```text
A ((black fluffy cat)) wearing a (white knitted scarf) walking through a city at night
```

#### Settings
```text
Steps: 100
Sampler: Euler a
CFG scale: 9
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 4
![BlackCatScarf04](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf04.png)

### Text Styling Tokens
Well, the cat is till mutated, but some of this is varying art styles blending together, so lets adjust the text and settings.  

#### Text Prompt
We will add token `artstation` and see what that gives us.  
```text
A ((black fluffy cat)) wearing a (white knitted scarf) walking through a city at night, artstation
```

#### Settings
We are increasing the inference steps to 150 but increasing the CFG value to 10 in order to be more accurate to the prompt.  
```text
Steps: 150
Sampler: Euler a
CFG scale: 10
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 5
![BlackCatScarf05](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf05.png)

### No ArtStation
Ah, that didn't do it for me. It's not a bad image by any means but not what I am looking for.  

#### Text Prompt
I am going to tweak the text prompt tokens to `8k, photo`, remove emphasis around (white knitted scarf) to `white knitted (scarf)`,
and replace `walking through` to `walking in`.  

```text 
A ((black fluffy cat)) wearing a white knitted (scarf) walking in a city at night, 8k, photo
```

#### Settings
We are decreasing the inference steps down to 90 and decreasing the CFG value to 8 in order to be more liberal from the prompt.  
```text
Steps: 90
Sampler: Euler a
CFG scale: 8
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 6
![BlackCatScarf06](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf06.png)

### Looking Good, Let's Add Weather
I am going to try and make some extra details and add resolution.  

#### Text Prompt
Here I am going to set the weather to snowy storm, streamline the sentence, and add desired details.  

```text
A ((black fluffy cat)) with yellow eyes wearing a red (scarf) sitting by a street light in a snowy city with neon lights at night, dark, 8k, high definition
```

#### Settings
Here I am playing with various steps to try and mix it up.  
```text
Steps: 65
Sampler: Euler a
CFG scale: 6.5
Seed: 559696028
Size: 1024x1024
Model hash: 2c02b20a
```

#### Output Image 7
![BlackCatScarf07](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf07.png)

### Perfect
This is kind of what I was looking for! I am going to take this same text and add steps now to see if we can increase the detail.  

#### Text Prompt
```text
A ((black fluffy cat)) with yellow eyes wearing a red (scarf) sitting by a street light in a snowy city with neon lights at night, dark, 8k, high definition
```

#### Settings
Going from 65 inference steps to 115. CFG up to 10.5 to try and lock this in.  
```text
Steps: 115
Sampler: Euler a
CFG scale: 10.5
Seed: 559696028
Size: 768x768
Model hash: 2c02b20a
```

#### Output Image 8
![BlackCatScarf08](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf08.png)

### Now, Let's Enhance
I am going to send this image to Img2Img to see if we can get a better resolution.

#### Text Prompt (Img2Img)
```text
A ((black fluffy cat)) with yellow eyes wearing a red (scarf) sitting by a street light in a snowy city with neon lights at night, dark, 8k, high definition
```

#### Negative Prompt (Img2Img)
```text
bad-artist
```

#### Settings (Img2Img)
Let's increase the resolution and detail.

```text
Steps: 120
Sampler: Euler a
CFG scale: 10.5
Seed: 559696028
Size: 1024x1024
Model hash: 2c02b20a
```

#### Output Image 9
![BlackCatScarf09](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf09.png)

### Extras!
Sending the output from 9 to Extras and 4x rescaling it.

#### Settings
```text
Upscale: 4
Visibility: 1.0
Model: R-ESRGAN 4x+ Anime6B
```

#### Output Image 10
<img src="https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/txtprompts/BlackCatScarf10.png" alt="animescaled" height="1024" width="1024" />


### Final Thoughts
It does take a while to get used to what works, what doesn't work. There's no silver bullet for great images it seems but it's also
not totally random. Increasing values, intending to increase the quality of the image, may throw off the image generation
counter-intuitively and end up with a worse output. So don't just go full max settings when you are in your discovery phase
(cherry picking the images you found pleasing). One thing that does help, v2 was trained on `768` and they simply provided the
best results from propmts currently. That means I save resolution adjustments until the image is "locked in" to what I am looking
for. Starting at 1024x1024 is doable but definitely giving me terrible results lately. I am not sure if it's my prompts or of its
gotten a bit buggier the last few days or not. I am always grabbing latest on AUTOMATIC1111 repo but this is a potential downside to
that... :)  