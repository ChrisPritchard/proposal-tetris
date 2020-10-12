# Proposal Tetris

A tetris clone I used to surprise propose to my Wife.

**NOTE: This is an old project (well, like five/six years prior to writing), written in XAML and a beta version of the Win2D graphics library - it will likely be very hard to compile. It was also written in Visual Studio 2013.**


How does it work? Its standard tetris - I had my girlfriend at the time testing it for me, as she likes tetris. She made some suggestions on how the controls would work better. I waited a week, fixed up the controls with her suggestions, and added the 'secret': when she passed the first level, it dropped a custom block with 'Will you marry me?' written on it, while the wedding march played in the background :) While she was absorbing that I presented the ring and a bottle of champaign. Not tooo bad, if I say so myself :D

## Re-use

The code is mainly framework agnostic and resides in [Game.cs](./game.cs) (along with the partial class Game_Special.cs which contains some of the proposal stuff) and this code is fairly good quality, though a few versions of C# back so some of its contents can be done better today. If you wanted to use it, you should be able to just rewrite the draw method for whatever canvas implementation you have.

## License notes

The code is MIT, however it uses a font (coders crux) which is CC4. There is also the wedding march tune whose providence I can't remember, so probably best not to use it.
