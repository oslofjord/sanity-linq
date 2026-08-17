import {defineConfig} from 'sanity'
import {structureTool} from 'sanity/structure'
import {visionTool} from '@sanity/vision'
import {schemaTypes} from './schemas'

export default defineConfig({
  name: 'default',
  title: 'Sanity LINQ Demo',
  projectId: 'dnjvf98k',
  dataset: 'test',
  plugins: [structureTool(), visionTool()],
  schema: {
    types: schemaTypes,
  },
})
