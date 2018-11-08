import { supportedLanguages } from '../config'
  
export default {
  name: 'localeString',
  type: 'object',
  title: 'String',
  fieldsets: [
    {
      title: 'Translations',
      name: 'translations',
      options: {collapsible: true}
    }
  ],
  fields: supportedLanguages.map(lang => (
    {
      title: lang.title,
      name: lang.id,
      type: 'string',
      fieldset: lang.isDefault ? null : 'translations'
    }
  ))
}
