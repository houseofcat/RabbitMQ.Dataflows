### Introduction to Inpainting!

#### Intro  

I have written a guide for setting up AUTOMATIC1111's stable diffusion locally over 
[here](https://houseofcat.io/guides/ml/stablediffusion/setuplocally). This is a quick tutorial 
on tweaking a part of an image you aren't too happy with.

#### AUTOMATIC1111's WEB UI - Seeding your Inpaint
The first thing we need is a solid text prompt to begin generating images. I plan on running
the following prompts without seed first, finding an image in the spirit of what I am
looking for. Then added the seed to the settings below. It took 3 more batches of
four before I found the image I selected.

#### Text Prompt
```text
Picture of (((John Wick))) running through a rainy city ((holding a gun)) 
in his right hand, 8k, high definition, digital art, artstation
```

#### Negative
```text
((((ugly)))), (((duplicate))), ((morbid)), ((mutilated)), out of frame,
extra fingers, mutated hands, ((poorly drawn hands)), ((poorly drawn face)),
(((mutation))), (((deformed))), ((ugly)), blurry, ((bad anatomy)),
(((bad proportions))), ((extra limbs)), cloned face, (((disfigured))),
out of frame, ugly, extra limbs, (bad anatomy), gross proportions,
(malformed limbs), ((missing arms)), ((missing legs)), (((extra arms))),
(((extra legs))), mutated hands, (fused fingers), (too many fingers),
(((long neck))), bad-artist, (too many eyes), (too few eyes)
```

#### Settings
```text
Steps: 40
Width: 768
Height: 768
Batch Size: 4
CFG Scale: 10
Seed: 4219356135
```

#### Output Image 1 - Starter/Seed Image

![JohnWick](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-01.png)

### Analyze Whats Wrong With The Image
When I first started playing with generation I was getting hundreds and hundreds of
batches rendered and trying to cherry pick the best images. I quickly realize
this wasn't a feasible long term approach because its taking to long to generate
something not completely crap. The more I tried to lock in values while tweaking others
others the harder it seemed to generate images I personaly liked. What I kept discovering
were there were dozens of images I discard because of a defect. A hand, an extra eye,
or a dozen extra eyes, etc. This quick introduction to inpaint may help you over come
some things - but don't expect miracles. The miracles are totally at random.

The above image is wrong in a few places. I generally like the overall look, it needs
more steps and obviously resolution scaling to 1024x1024.

1. We need to fix that monstrosity in his hand.
2. The sign extends too far out.
2. What is going on around his hair in the back?
3. That bottom left front of his suit jacket looks off.

### Send To Inpaint
Send to Inpaint!

![Send to Inpaint](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-02.png)

We are going to use the same settings but adjust our new inpainting options a bit.

![Settings](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-03.png)

1. Shrink the brush, less is often more, but there is a undo button next to
the X.
2. Start with a `Mask blur` of 2.
3. Set it to `fill` in content for now.
4. Optionally if you are inpainting at full resolution there is a slider to increase or
decrease how many pixels it will adjust accordingly. If you feel that the changes to
area are too severe try decreasing the value a bit.

#### Text Prompt
```text
Picture of (((John Wick))) running through a rainy city ((holding a gun)) 
in his right hand, 8k, high definition, digital art, artstation
```

#### Negative
```text
((((ugly)))), (((duplicate))), ((morbid)), ((mutilated)), out of frame,
extra fingers, mutated hands, ((poorly drawn hands)), ((poorly drawn face)),
(((mutation))), (((deformed))), ((ugly)), blurry, ((bad anatomy)),
(((bad proportions))), ((extra limbs)), cloned face, (((disfigured))),
out of frame, ugly, extra limbs, (bad anatomy), gross proportions,
(malformed limbs), ((missing arms)), ((missing legs)), (((extra arms))),
(((extra legs))), mutated hands, (fused fingers), (too many fingers),
(((long neck))), bad-artist, (too many eyes), (too few eyes)
```

#### Settings
```text
Steps: 50
Width: 768
Height: 768
Batch Size: 4
CFG Scale: 10
Denoising Strengh: 0.75
Seed: 4219356135
```

### Mistake Number One
Trying to change too much all at once. Select just one thing we are going to work on. I
save the most complicated (and ultimately the most frustrating ones) till last then feed
back into the inpainting.

![Mistake Number One](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-04.png)

Let's do our first batch of images generated. This is not cherry picked.

![OutputGrid](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-05.png)

The bottom right image is perfect (to me of course). We are going to send this back in
to inpainting! Click the `X` to quickly clear the previous "masking".

![SendToInpaint2](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-06.png)

### Output Image 2 - Sign Creation
![OutputImage2](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-06b.png)

### Send To Inpaint 2
Now we the have the first part fixed, I am going to try and redraw the front left of the suit
with varying degrees of increasing masking. It will be difficult to see since it is on
such a dark image.

![New Masking](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-07.png)

### Output Grid - All Same Settings
Here is the output. The 3rd image (bottom left) is the one I like.
![OutputGrid2](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-08.png)

### Output Image 3 - Suit Correction
![SendToInpaint3](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-08b.png)

### Send To Inpaint 3
Send the previous image back to inpaint after clearing the masking. And lets start working
on the gun.

I am going to test a reduction in size first by paintin a whole new area in there.
![NoGunMask](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-09a.png)

What I am hoping to generate is a new seed for inpainting but I need a smaller item. I was 
able to generate an image in my first batch.
![Gun1](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-09b.png)

Back to inpaint!
![NoGunMask2](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-09c.png)

![Gun2](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-09d.png)

### Tweaking aka Send to Inpaint 97 Times
I am kidding but now we are pretty close. We need to adjust the `Denoising` value. This restricts
how much it will diverge from the current image.

#### Settings
```text
Steps: 50
Width: 768
Height: 768
Batch Size: 4
CFG Scale: 12
Denoising Strengh: 0.5
Seed: 4219356135
```

#### Output Image 4
![NoGunMask3](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-10a.png)

![Gun3](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-10b.png)

You want to keepdoing this until you are satisfied! You may want to increase or decrease your
denoising values if the results are too dramatic or not dramatic enough. Lower noise can
also help disapear parts into a blur.  

### Step 11 - Fixed Gun Image 5
I kept repeating, feeding back to inpainting, and adjusting the mask. I believe it took
another 4 or 5 attempts to get it to this.

![Gun4](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-11.png)

### Fix the Remaining Oddities
The more you look at something, the more you can start spotting the imperfections.
The Tie, the car wheels?!, that hing behind his head???  

Remember, more noise for changes, lowest noise to erase, medium to sort of tweak it in
or lock it in, large means lots of variation. Denoise here is varying between steps.   

I will do the tie first then the behind the head.  

#### Settings
```text
Steps: 50
Width: 768
Height: 768
Batch Size: 4
CFG Scale: 12
Denoising Strengh: 0.5 
Seed: 4219356135
```

#### Before
![Shoulder](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-12a.png)

#### After
![Car Wheels](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-12b.png)

### Send to Img2Img - Final Resolution by Batch
I am now going to port this to 1024x1024, with varying inference steps to try and cleanup
detail. I am shrinking the batch size to 2. I will start with 50 inference steps and increase
in increments of 5 until I hate it. Feeding the image back into the Img2Img prompt or inpainting
as I see fit.

#### Settings
```text
Steps: 50
Width: 1024
Height: 1024
Batch Size: 4
CFG Scale: 12
Denoising Strengh: 0.1
Seed: 4219356135
```

#### Before
![Step12](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-12a.png)

#### After
![Step13](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-13.png)

### Inpainting - The Face
Now that we are dealing with `1024x1024`, I am going to clean up some details around
Keanu Reeves face. This is virtually impossible to do from the distance John Wick is
from the "camera". Not enough surface area to make meaningful changes. You could mask
his entire face, set Denoise to `0.75` and start playing face roulette. I am not going
to do that for this guide. I am going to clean up some of the rougher parts. Fixing
one section at a time and then fixing the next one.  

#### Settings
```text
Steps: 65 (increase if missing detail, decrease if I feel the change is too grainy)
Width: 1024
Height: 1024
Batch Size: 2
CFG Scale: 9.5 (minor variation)
Denoising Strengh: 0.6 (major variation depending on what I am doing)
Seed: 4219356135
```

#### Before
![Step13A](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-13a.png)

#### After
![Step13B](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-13b.png)


### Upscale with R-ESRGAN 4x+ Anime6B
This should suffice for the purposes of the guide. Now we upscale and finish it.  

#### Start
![JohnWick](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/step-01.png)

#### Finished
![JWFinished](https://houseofcat.blob.core.windows.net/website/guides/ml/stablediffusion/inpainting/john_wick_finished.png)