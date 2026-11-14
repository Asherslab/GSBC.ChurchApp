export function saveFile(name, contentType, bytes) {
  const link = document.createElement("a");
  link.href = URL.createObjectURL(new Blob([bytes], { type: contentType }));
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(link.href), 60000);
}
