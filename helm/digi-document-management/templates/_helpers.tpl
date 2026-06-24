{{- define "dm.name" -}}digi-document-management{{- end -}}
{{- define "dm.labels" -}}
app.kubernetes.io/name: {{ include "dm.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/part-of: docportal
{{- end -}}
