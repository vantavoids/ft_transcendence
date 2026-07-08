package email

import (
	"bytes"
	"embed"
	"fmt"
	htmltemplate "html/template"
	texttemplate "text/template"
)

var templatesFS embed.FS

var (
	htmlTemplates = htmltemplate.Must(
		htmltemplate.ParseFS(templatesFS, "templates/*.html"))
	textTemplates = texttemplate.Must(
		texttemplate.ParseFS(templatesFS, "templates/*.txt"))
)

func render(name string, data any) (text string, html string, err error) {
	var tbuf, hbuf bytes.Buffer

	if err = textTemplates.ExecuteTemplate(&tbuf, name+".txt", data); err != nil {
		return "", "", fmt.Errorf("render text %q: %w", name, err)
	}

	if err = htmlTemplates.ExecuteTemplate(&hbuf, name+".html", data); err != nil {
		return "", "", fmt.Errorf("render html %q: %w", name, err)
	}

	return tbuf.String(), hbuf.String(), nil
}
