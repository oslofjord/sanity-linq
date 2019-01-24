
export default {
    name: 'tabletest',
    title: 'Table Test',
    type: 'document',
    fieldsets: [
      {
        title: 'Bootstrap',
        name: 'bootstrap',
        options: {collapsible: true}
      }
    ],
    fields: [
      {
        name: 'title',
        title: 'Title',
        type: 'localeString'
      },
      {
        name: 'bootstrap',
        title: 'Bootstrap',
        type: 'boolean',
        fieldset: 'bootstrap'
      },
      {
        name: 'options',
        title: 'Options',
        type: 'localeString'
      },
      {
        name: 'myAwesomeTable',
        type: 'table'
      }
    ]
  }

