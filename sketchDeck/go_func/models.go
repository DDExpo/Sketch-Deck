package go_func

var ThumbSizesTypes = [5]string{"small-view", "medium-view", "large-view", "gigantic-view", "details-view"}

type thumbSize struct {
	Width  int
	Height int
}

type ImagesWithThumbnails struct {
	Image  string
	Name   string
	Date   string
	Thumbs map[string]string
}

var ThumbSizes = struct {
	SmallIcon    thumbSize
	MediumIcon   thumbSize
	LargeIcon    thumbSize
	GiganticIcon thumbSize
	DetailsIcon  thumbSize
}{
	SmallIcon:    thumbSize{Width: 90, Height: 90},
	MediumIcon:   thumbSize{Width: 150, Height: 150},
	LargeIcon:    thumbSize{Width: 226, Height: 226},
	GiganticIcon: thumbSize{Width: 600, Height: 600},
	DetailsIcon:  thumbSize{Width: 26, Height: 26},
}
